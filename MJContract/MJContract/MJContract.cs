using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using System;
using System.ComponentModel;
using System.Numerics;

namespace MJContract
{
    public class MJContract : SmartContract
    {
        delegate object deleDyncall(string method, object[] arr);

        public delegate void deleTransfer(byte[] from, byte[] to, BigInteger value);

        [DisplayName("transfer")]
        public static event deleTransfer Transferred;
        public delegate void deleRefundTarget(byte[] txid, byte[] who);
        [DisplayName("onRefundTarget")]
        public static event deleRefundTarget onRefundTarget;

        private static readonly byte[] gas_asset_id = Neo.SmartContract.Framework.Helper.HexToBytes("e72d286979ee6cb1b7e65dfddfb2e384100b8d148e7758de42e4168b71792c60");

        public static readonly byte[] ContractOwner = "ANQUmFnn9psTnCYSSZ5nuNCGZ1LWSmwHF4".ToScriptHash();

        public static readonly byte[] MintOwner = "ANQUmFnn9psTnCYSSZ5nuNCGZ1LWSmwHF4".ToScriptHash();

        private const ulong supplytotalcount = 99999999;

        public static string symbol()
        {
            return "MJC";
        }
        public static string Name()
        {
            return "MJC";
        }
        public static byte decimals()
        {
            return 0;
        }
        public static string Version()
        {
            return "1.0.0";
        }
        public class TransferInfo
        {
            public byte[] from;
            public byte[] to;
            public BigInteger value;
        }

        private static byte[] concatKey(string s1, string s2)
        {
            return s1.AsByteArray().Concat(s2.AsByteArray());
        }
        public static BigInteger _totalSupply()
        {
            return Storage.Get(Storage.CurrentContext, "totalSupply").AsBigInteger();
        }
        public static BigInteger totalSupply()
        {
            return Storage.Get(Storage.CurrentContext, "totalExchargeMJC").AsBigInteger();
        }
        public static BigInteger Supplying()
        {
            return Storage.Get(Storage.CurrentContext, "supplying").AsBigInteger();
        }
        private static void _subSupplying(BigInteger count)
        {
            BigInteger supplying = Storage.Get(Storage.CurrentContext, "supplying").AsBigInteger();
            supplying -= count;
            if (supplying > 0)
            {
                Storage.Put(Storage.CurrentContext, "supplying", supplying);
            }
            else
            {
                Storage.Delete(Storage.CurrentContext, "supplying");
            }
        }
        private static void _addSupplying(BigInteger count)
        {
            BigInteger supplying = Storage.Get(Storage.CurrentContext, "supplying").AsBigInteger();
            supplying += count;
            Storage.Put(Storage.CurrentContext, "supplying", supplying);
        }
        public static bool Deploy()
        {
            byte[] total_supply = Storage.Get(Storage.CurrentContext, "totalExchargeMJC");
            if (total_supply.Length != 0) return false;
            Storage.Put(Storage.CurrentContext, ContractOwner, supplytotalcount);
            Storage.Put(Storage.CurrentContext, "totalExchargeMJC", supplytotalcount);
            Transferred(null, ContractOwner, supplytotalcount);
            return true;
        }
        public static BigInteger balanceOf(byte[] account)
        {
            return Storage.Get(Storage.CurrentContext, account).AsBigInteger();
        }
        public static bool mintTokens()
        {
            var tx = ExecutionEngine.ScriptContainer as Transaction;

            byte[] who = null;
            TransactionOutput[] reference = tx.GetReferences();
            for (var i = 0; i < reference.Length; i++)
            {
                if (reference[i].AssetId.AsBigInteger() == gas_asset_id.AsBigInteger())
                {
                    who = reference[i].ScriptHash;
                    break;
                }
            }

            var lastTx = Storage.Get(Storage.CurrentContext, "lastTx");
            if (lastTx.Length > 0 && tx.Hash == lastTx)
                return false;
            Storage.Put(Storage.CurrentContext, "lastTx", tx.Hash);

            TransactionOutput[] outputs = tx.GetOutputs();
            ulong value = 0;
            foreach (TransactionOutput output in outputs)
            {
                if (output.ScriptHash == ExecutionEngine.ExecutingScriptHash &&
                    output.AssetId.AsBigInteger() == gas_asset_id.AsBigInteger())
                {
                    value += (ulong)output.Value;
                }
            }
            value = value / 100000000;
            var total_supply = Storage.Get(Storage.CurrentContext, "totalSupply").AsBigInteger();
            total_supply += value;
            Storage.Put(Storage.CurrentContext, "totalSupply", total_supply);
            _saveGas(who, value, 0);
            return transfer(ContractOwner, who, value);
        }
        public static bool refund(byte[] who)
        {
            var tx = ExecutionEngine.ScriptContainer as Transaction;
            var outputs = tx.GetOutputs();
            if (outputs[0].AssetId.AsBigInteger() != gas_asset_id.AsBigInteger())
                return false;
            if (outputs[0].ScriptHash.AsBigInteger() != ExecutionEngine.ExecutingScriptHash.AsBigInteger())
                return false;

            byte[] target = getRefundTarget(tx.Hash);
            if (target.Length > 0)
                return false;

            var count = outputs[0].Value;
            count = count / 100000000;
            bool b = transfer(who, ContractOwner, count);
            if (!b)
                return false;

            byte[] coinid = tx.Hash.Concat(new byte[] { 0, 0 });
            Storage.Put(Storage.CurrentContext, coinid, who);
            onRefundTarget(tx.Hash, who);

            var total_supply = Storage.Get(Storage.CurrentContext, "totalSupply").AsBigInteger();
            total_supply -= count;
            Storage.Put(Storage.CurrentContext, "totalSupply", total_supply);
            _saveGas(who, count, 1);
            return true;
        }

        public static byte[] getRefundTarget(byte[] txid)
        {
            byte[] coinid = txid.Concat(new byte[] { 0, 0 });
            byte[] target = Storage.Get(Storage.CurrentContext, coinid);
            return target;
        }
        public static void _saveGas(byte[] account, BigInteger value, BigInteger type)
        {
            var key = concatKey("gas/", account.AsString());
            var userGas = Storage.Get(Storage.CurrentContext, key).AsBigInteger();
            if (type == 0)
            {
                userGas += value;
            }
            else
            {
                userGas -= value;
            }
            Storage.Put(Storage.CurrentContext, key, userGas);
        }
        public static BigInteger _getGas(byte[] account)
        {
            var key = concatKey("gas/", account.AsString());
            return Storage.Get(Storage.CurrentContext, key).AsBigInteger();
        }
        public static bool transfer(byte[] from, byte[] to, BigInteger value)
        {
            if (value <= 0) return false;
            value = value * 10;
            if (from.Length > 0)
            {
                BigInteger from_value = Storage.Get(Storage.CurrentContext, from).AsBigInteger();
                if (from_value < value) return false;
                if (from_value == value)
                    Storage.Delete(Storage.CurrentContext, from);
                else
                    Storage.Put(Storage.CurrentContext, from, from_value - value);
            }
            if (to.Length > 0)
            {
                BigInteger to_value = Storage.Get(Storage.CurrentContext, to).AsBigInteger();
                Storage.Put(Storage.CurrentContext, to, to_value + value);
            }
            setTxInfo(from, to, value);
            Transferred(from, to, value);
            saveSupply(from, to, value);
            return true;
        }
        public static bool transferFrom_app(byte[] from, byte[] to, BigInteger value)
        {
            if (value <= 0) return false;
            if (from.Length > 0)
            {
                BigInteger from_value = Storage.Get(Storage.CurrentContext, from).AsBigInteger();
                if (from_value < value) return false;
                if (from_value == value)
                    Storage.Delete(Storage.CurrentContext, from);
                else
                    Storage.Put(Storage.CurrentContext, from, from_value - value);
            }
            if (to.Length > 0)
            {
                BigInteger to_value = Storage.Get(Storage.CurrentContext, to).AsBigInteger();
                Storage.Put(Storage.CurrentContext, to, to_value + value);
            }
            setTxInfo(from, to, value);
            Transferred(from, to, value);
            saveSupply(from, to, value);
            return true;
        }
        static readonly byte[] doublezero = new byte[2] { 0x00, 0x00 };
        private static void saveSupply(byte[] from, byte[] to, BigInteger value)
        {
            if (from == ContractOwner)
            {
                _addSupplying(value);
            }
            else if (to == ContractOwner)
            {
                _subSupplying(value);
            }
        }
        private static void setTxInfo(byte[] from, byte[] to, BigInteger value)
        {
            TransferInfo info = new TransferInfo();
            info.from = from;
            info.to = to;
            info.value = value;

            var data = info.from;
            var lendata = ((BigInteger)data.Length).AsByteArray().Concat(doublezero).Range(0, 2);
            var txinfo = lendata.Concat(data);

            data = info.to;
            lendata = ((BigInteger)data.Length).AsByteArray().Concat(doublezero).Range(0, 2);
            txinfo = txinfo.Concat(lendata).Concat(data);

            data = value.AsByteArray();
            lendata = ((BigInteger)data.Length).AsByteArray().Concat(doublezero).Range(0, 2);
            txinfo = txinfo.Concat(lendata).Concat(data);

            var txid = (ExecutionEngine.ScriptContainer as Transaction).Hash;
            var keytxid = new byte[] { 0x12 }.Concat(txid);
            Storage.Put(Storage.CurrentContext, keytxid, txinfo);
        }

        public static TransferInfo getTXInfo(byte[] txid)
        {
            byte[] keytxid = new byte[] { 0x12 }.Concat(txid);
            byte[] v = Storage.Get(Storage.CurrentContext, keytxid);
            if (v.Length == 0)
                return null;

            TransferInfo info = new TransferInfo();
            int seek = 0;
            var fromlen = (int)v.Range(seek, 2).AsBigInteger();
            seek += 2;
            info.from = v.Range(seek, fromlen);
            seek += fromlen;
            var tolen = (int)v.Range(seek, 2).AsBigInteger();
            seek += 2;
            info.to = v.Range(seek, tolen);
            seek += tolen;
            var valuelen = (int)v.Range(seek, 2).AsBigInteger();
            seek += 2;
            info.value = v.Range(seek, valuelen).AsBigInteger();
            return info;
        }

        public static bool consumeDiamond(byte[] sender, BigInteger diamondcount)
        {
            if (sender.Length != 20) return false;
            if (!Runtime.CheckWitness(sender)) return false;
            return transferFrom_app(sender, ContractOwner, diamondcount);
        }
        public static bool setInfo(string type, string roomroundid, string hash)
        {
            var key = concatKey(type, roomroundid);
            Storage.Put(Storage.CurrentContext, key, hash);
            return true;
        }
        public static string getInfo(string type, string roomroundid)
        {
            var key = concatKey(type, roomroundid);
            var str = Storage.Get(Storage.CurrentContext, key).AsString();
            return str;
        }

        public static Object Main(string method, params object[] args)
        {
            if (Runtime.Trigger == TriggerType.Verification)
            {
                var tx = ExecutionEngine.ScriptContainer as Transaction;
                var curhash = ExecutionEngine.ExecutingScriptHash;
                var inputs = tx.GetInputs();
                var outputs = tx.GetOutputs();

                for (var i = 0; i < inputs.Length; i++)
                {
                    byte[] coinid = inputs[i].PrevHash.Concat(new byte[] { 0, 0 });
                    if (inputs[i].PrevIndex == 0)
                    {
                        byte[] target = Storage.Get(Storage.CurrentContext, coinid);
                        if (target.Length > 0)
                        {
                            if (inputs.Length > 1 || outputs.Length != 1)
                                return false;

                            if (outputs[0].ScriptHash.AsBigInteger() == target.AsBigInteger())
                                return true;
                            else
                                return false;
                        }
                    }
                }
                var refs = tx.GetReferences();
                BigInteger inputcount = 0;
                for (var i = 0; i < refs.Length; i++)
                {
                    if (refs[i].AssetId.AsBigInteger() != gas_asset_id.AsBigInteger())
                        return false;

                    if (refs[i].ScriptHash.AsBigInteger() != curhash.AsBigInteger())
                        return false;

                    inputcount += refs[i].Value;
                }

                BigInteger outputcount = 0;
                for (var i = 0; i < outputs.Length; i++)
                {
                    if (outputs[i].ScriptHash.AsBigInteger() != curhash.AsBigInteger())
                    {
                        return false;
                    }
                    outputcount += outputs[i].Value;
                }
                if (outputcount != inputcount)
                    return false;

                return true;
            }
            else if (Runtime.Trigger == TriggerType.VerificationR)
            {
                return true;
            }
            else if (Runtime.Trigger == TriggerType.Application)
            {
                var callscript = ExecutionEngine.CallingScriptHash;

                if (method == "_getGas")
                {
                    if (args.Length != 1) return 0;
                    byte[] account = (byte[])args[0];
                    return _getGas(account);
                }
                if (method == "deploy")
                {
                    return Deploy();
                }
                if (method == "_totalSupply") return _totalSupply();
                if (method == "totalSupply") return totalSupply();
                if (method == "version") return Version();
                if (method == "name") return Name();
                if (method == "decimals") return decimals();
                if (method == "balanceOf")
                {
                    if (args.Length != 1) return 0;
                    byte[] account = (byte[])args[0];
                    return balanceOf(account);
                }
                if (method == "mintTokens")
                {
                    if (args.Length != 0) return 0;
                    return mintTokens();
                }
                if (method == "refund")
                {
                    if (args.Length != 1) return 0;
                    byte[] who = (byte[])args[0];
                    if (!Runtime.CheckWitness(who))
                        return false;
                    return refund(who);
                }
                if (method == "getRefundTarget")
                {
                    if (args.Length != 1) return 0;
                    byte[] hash = (byte[])args[0];
                    return getRefundTarget(hash);
                }
                if (method == "getTXInfo")
                {
                    if (args.Length != 1) return 0;
                    byte[] txid = (byte[])args[0];
                    return getTXInfo(txid);
                }
                if (method == "consumeDiamond")
                {
                    if (args.Length != 2) return 0;
                    byte[] owner = (byte[])args[0];
                    BigInteger diamondcount = (BigInteger)args[1];

                    return consumeDiamond(owner, diamondcount);
                }
                if (method == "setInfo")
                {
                    if (args.Length != 3) return 0;
                    string type = (string)args[0];
                    string roomroundid = (string)args[1];
                    string hash = (string)args[2];

                    return setInfo(type, roomroundid, hash);
                }
                if (method == "getInfo")
                {
                    if (args.Length != 2) return 0;
                    string type = (string)args[0];
                    string roomroundid = (string)args[1];

                    return getInfo(type, roomroundid);
                }
                #region 升级合约,耗费490,仅限管理员
                if (method == "upgrade")
                {
                    if (!Runtime.CheckWitness(ContractOwner))
                        return false;

                    if (args.Length != 1 && args.Length != 9)
                        return false;

                    byte[] script = Blockchain.GetContract(ExecutionEngine.ExecutingScriptHash).Script;
                    byte[] new_script = (byte[])args[0];
                    if (script == new_script)
                        return false;

                    byte[] parameter_list = new byte[] { 0x07, 0x10 };
                    byte return_type = 0x05;
                    bool need_storage = (bool)(object)05;
                    string name = "MJ";
                    string version = "1";
                    string author = "NEL";
                    string email = "0";
                    string description = "gas与sgas的互换";

                    if (args.Length == 9)
                    {
                        parameter_list = (byte[])args[1];
                        return_type = (byte)args[2];
                        need_storage = (bool)args[3];
                        name = (string)args[4];
                        version = (string)args[5];
                        author = (string)args[6];
                        email = (string)args[7];
                        description = (string)args[8];
                    }
                    Contract.Migrate(new_script, parameter_list, return_type, need_storage, name, version, author, email, description);
                    return true;
                }
                #endregion
            }
            return false;
        }

    }
}
