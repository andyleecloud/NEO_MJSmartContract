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

        public static readonly byte[] ContractOwner = "ANQUmFnn9psTnCYSSZ5nuNCGZ1LWSmwHF4".ToScriptHash();

        public static readonly byte[] MintOwner = "ANQUmFnn9psTnCYSSZ5nuNCGZ1LWSmwHF4".ToScriptHash();

        private const ulong supplytotalcount = 888888888;

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

        public static BigInteger totalExchargeSgas()
        {
            return Storage.Get(Storage.CurrentContext, "totalExchargeSgas").AsBigInteger();
        }

        private static void _addTotal(BigInteger count)
        {
            BigInteger total = Storage.Get(Storage.CurrentContext, "totalExchargeSgas").AsBigInteger();
            total += count;
            Storage.Put(Storage.CurrentContext, "totalExchargeSgas", total);
        }

        private static void _subTotal(BigInteger count)
        {
            BigInteger total = Storage.Get(Storage.CurrentContext, "totalExchargeSgas").AsBigInteger();
            total -= count;
            if (total > 0)
            {
                Storage.Put(Storage.CurrentContext, "totalExchargeSgas", total);
            }
            else
            {

                Storage.Delete(Storage.CurrentContext, "totalExchargeSgas");
            }
        }

        private static byte[] concatKey(string s1, string s2)
        {
            return s1.AsByteArray().Concat(s2.AsByteArray());
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
            Storage.Put(Storage.CurrentContext, ExecutionEngine.ExecutingScriptHash, supplytotalcount);
            Storage.Put(Storage.CurrentContext, "totalExchargeMJC", supplytotalcount);
            Transferred(null, ExecutionEngine.ExecutingScriptHash, supplytotalcount);
            return true;
        }

        public static BigInteger balanceOf_Sgas(byte[] address)
        {
            var key = new byte[] { 0x11 }.Concat(address);
            return Storage.Get(Storage.CurrentContext, key).AsBigInteger();
        }

        public static BigInteger balanceOf(byte[] account)
        {
            return Storage.Get(Storage.CurrentContext, account).AsBigInteger();
        }

        public static bool transfer(byte[] from, byte[] to, BigInteger value)
        {
            if (value <= 0) return false;
            if (from.Length != 20) return false;
            if (to.Length != 20) return false;

            BigInteger from_value = Storage.Get(Storage.CurrentContext, from).AsBigInteger();
            if (from_value < value) return false;
            if (from == to) return true;
            if (from_value == value)
                Storage.Delete(Storage.CurrentContext, from);
            else
                Storage.Put(Storage.CurrentContext, from, from_value - value);

            BigInteger to_value = Storage.Get(Storage.CurrentContext, to).AsBigInteger();
            Storage.Put(Storage.CurrentContext, to, to_value + value);

            setTxInfo(from, to, value);
            Transferred(from, to, value);
            return true;
        }

        static readonly byte[] doublezero = new byte[2] { 0x00, 0x00 };

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
            //return Helper.Deserialize(v) as TransferInfo;
        }

        public static bool changeSupply(byte[] txid)
        {
            var transferinfo = getTXInfo(txid);
            if (transferinfo == null) return false;
            if (transferinfo.from == transferinfo.to) return false;

            if (transferinfo.from == ExecutionEngine.ExecutingScriptHash)
                _addSupplying(transferinfo.value);

            if (transferinfo.to == ExecutionEngine.ExecutingScriptHash)
                _subSupplying(transferinfo.value);

            return true;
        }

        public static bool hasAlreadyCharged(byte[] txid)
        {
            byte[] keytxid = new byte[] { 0x12 }.Concat(txid);
            byte[] txinfo = Storage.Get(Storage.CurrentContext, keytxid);
            if (txinfo.Length > 0)
            {
                return false;
            }
            return true;
        }

        public static bool rechargeToken(byte[] owner, byte[] txid)
        {
            if (owner.Length != 20)
            {
                Runtime.Log("Owner error.");
                return false;
            }
            var keytxid = new byte[] { 0x12 }.Concat(txid);
            byte[] txinfo = Storage.Get(Storage.CurrentContext, keytxid);
            if (txinfo.Length > 0) return false;

            object[] args = new object[1] { txid };
            byte[] sgasHash = Storage.Get(Storage.CurrentContext, "sgas");
            deleDyncall dyncall = (deleDyncall)sgasHash.ToDelegate();
            object[] res = (object[])dyncall("getTXInfo", args);

            if (res.Length > 0)
            {
                byte[] from = (byte[])res[0];
                byte[] to = (byte[])res[1];
                BigInteger value = (BigInteger)res[2];

                if (from == owner)
                {
                    if (to == ExecutionEngine.ExecutingScriptHash)
                    {
                        Storage.Put(Storage.CurrentContext, keytxid, value);

                        BigInteger nMoney = 0;
                        var keytowner = new byte[] { 0x11 }.Concat(owner);
                        byte[] ownerMoney = Storage.Get(Storage.CurrentContext, keytowner);
                        if (ownerMoney.Length > 0)
                        {
                            nMoney = ownerMoney.AsBigInteger();
                        }
                        nMoney += value;

                        _addTotal(value);

                        Storage.Put(Storage.CurrentContext, keytowner, nMoney.AsByteArray());
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool drawToken(byte[] sender, BigInteger count)
        {
            if (sender.Length != 20)
            {
                Runtime.Log("Owner error.");
                return false;
            }
            var keytsender = new byte[] { 0x11 }.Concat(sender);

            if (Runtime.CheckWitness(sender))
            {
                BigInteger nMoney = 0;
                byte[] ownerMoney = Storage.Get(Storage.CurrentContext, keytsender);
                if (ownerMoney.Length > 0)
                {
                    nMoney = ownerMoney.AsBigInteger();
                }
                if (count <= 0 || count > nMoney)
                {

                    count = nMoney;
                }

                object[] args = new object[3] { ExecutionEngine.ExecutingScriptHash, sender, count };
                byte[] sgasHash = Storage.Get(Storage.CurrentContext, "sgas");
                deleDyncall dyncall = (deleDyncall)sgasHash.ToDelegate();
                bool res = (bool)dyncall("transfer_app", args);
                if (!res)
                {
                    return false;
                }

                nMoney -= count;

                _subTotal(count);

                if (nMoney > 0)
                {
                    Storage.Put(Storage.CurrentContext, keytsender, nMoney.AsByteArray());
                }
                else
                {
                    Storage.Delete(Storage.CurrentContext, keytsender);
                }

                return true;
            }
            return false;
        }

        public static bool drawToContractOwner(BigInteger count)
        {
            if (Runtime.CheckWitness(ContractOwner))
            {
                BigInteger nMoney = 0;

                object[] args = new object[1] { ExecutionEngine.ExecutingScriptHash };
                byte[] sgasHash = Storage.Get(Storage.CurrentContext, "sgas");
                deleDyncall dyncall = (deleDyncall)sgasHash.ToDelegate();
                BigInteger totalMoney = (BigInteger)dyncall("balanceOf", args);
                BigInteger supplyMoney = Storage.Get(Storage.CurrentContext, "totalExchargeSgas").AsBigInteger();

                BigInteger canDrawMax = totalMoney - supplyMoney;
                if (count <= 0 || count > canDrawMax)
                {
                    count = canDrawMax;
                }

                args = new object[3] { ExecutionEngine.ExecutingScriptHash, ContractOwner, count };

                deleDyncall dyncall2 = (deleDyncall)sgasHash.ToDelegate();
                bool res = (bool)dyncall2("transfer_app", args);
                if (!res)
                {
                    return false;
                }

                _subTotal(count);
                return true;
            }
            return false;
        }

        public static bool buyDiamond(byte[] sender, BigInteger sgascount, BigInteger diamondcount)
        {
            if (sender.Length != 20) return false;
            if (!Runtime.CheckWitness(sender)) return false;
            var keytsender = new byte[] { 0x11 }.Concat(sender);
            BigInteger nMoney = 0;
            byte[] ownerMoney = Storage.Get(Storage.CurrentContext, keytsender);
            if (ownerMoney.Length > 0)
            {
                nMoney = ownerMoney.AsBigInteger();
            }
            else
            {
                nMoney = 0;
            }
            if (nMoney < sgascount)
            {
                return false;
            }
            nMoney -= sgascount;
            var args = new object[3] { ExecutionEngine.ExecutingScriptHash, ContractOwner, sgascount };
            byte[] sgasHash = Storage.Get(Storage.CurrentContext, "sgas");
            deleDyncall dyncall2 = (deleDyncall)sgasHash.ToDelegate();
            bool res = (bool)dyncall2("transfer_app", args);
            if (!res) return false;

            _subTotal(sgascount);
            if (nMoney > 0)
            {
                Storage.Put(Storage.CurrentContext, keytsender, nMoney.AsByteArray());
            }
            else
            {
                Storage.Delete(Storage.CurrentContext, keytsender);
            }
            return transfer(ExecutionEngine.ExecutingScriptHash, sender, diamondcount);
        }

        public static bool consumeDiamond(byte[] sender, BigInteger diamondcount)
        {
            if (sender.Length != 20) return false;
            if (!Runtime.CheckWitness(sender)) return false;
            BigInteger nMoney = 0;
            byte[] ownerMoney = Storage.Get(Storage.CurrentContext, sender);
            if (ownerMoney.Length > 0)
            {
                nMoney = ownerMoney.AsBigInteger();
            }
            else
            {
                nMoney = 0;
            }

            if (nMoney < diamondcount) return false;
            return transfer(sender, ExecutionEngine.ExecutingScriptHash, diamondcount);
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
                if (ContractOwner.Length == 20)
                {
                    return Runtime.CheckWitness(ContractOwner);
                }
                else if (ContractOwner.Length == 33)
                {
                    byte[] signature = method.AsByteArray();
                    return VerifySignature(signature, ContractOwner);
                }
            }
            else if (Runtime.Trigger == TriggerType.VerificationR)
            {
                return true;
            }
            else if (Runtime.Trigger == TriggerType.Application)
            {
                var callscript = ExecutionEngine.CallingScriptHash;

                if (method == "_setSgas")
                {
                    if (!Runtime.CheckWitness(ContractOwner)) return false;
                    Storage.Put(Storage.CurrentContext, "sgas", (byte[])args[0]);
                    return true;
                }
                if (method == "_getSgas")
                {
                    return Storage.Get(Storage.CurrentContext, "sgas");
                }
                if (method == "deploy")
                {
                    if (!Runtime.CheckWitness(ContractOwner)) return false;
                    return Deploy();
                }
                if (method == "totalExchargeSgas") return totalExchargeSgas();
                if (method == "totalSupply") return totalSupply();
                if (method == "version") return Version();
                if (method == "name") return Name();
                if (method == "decimals") return decimals();
                if (method == "balanceOf")
                {
                    if (args.Length != 1) return false;
                    byte[] account = (byte[])args[0];

                    return balanceOf(account);
                }
                if (method == "balanceOf_Sgas")
                {
                    if (args.Length != 1) return false;
                    byte[] account = (byte[])args[0];

                    return balanceOf_Sgas(account);
                }
                if (method == "transfer")
                {
                    if (args.Length != 3) return false;

                    var from = (byte[])args[0];
                    var to = (byte[])args[1];
                    var value = (BigInteger)args[2];

                    if (!Runtime.CheckWitness(from)) return false;
                    if (ExecutionEngine.EntryScriptHash.AsBigInteger() != callscript.AsBigInteger()) return false;

                    return transfer(from, to, value);
                }
                if (method == "transferFrom_app")
                {
                    if (args.Length != 3) return false;

                    byte[] from = (byte[])args[0];
                    byte[] to = (byte[])args[1];
                    BigInteger tokenId = (BigInteger)args[2];

                    if (!Runtime.CheckWitness(from)) return false;
                    if (to != ContractOwner) return false;

                    return transfer(from, to, tokenId);
                }
                if (method == "getTXInfo")
                {
                    if (args.Length != 1) return false;
                    byte[] txid = (byte[])args[0];

                    return getTXInfo(txid);
                }
                if (method == "changeSupply")
                {
                    if (args.Length != 1) return false;
                    byte[] txid = (byte[])args[0];

                    return changeSupply(txid);
                }
                if (method == "drawToken")
                {
                    if (args.Length != 2) return false;
                    byte[] owner = (byte[])args[0];
                    BigInteger count = (BigInteger)args[1];

                    return drawToken(owner, count);
                }
                if (method == "drawToContractOwner")
                {
                    if (args.Length != 1) return false;
                    BigInteger count = (BigInteger)args[0];

                    return drawToContractOwner(count);
                }
                if (method == "rechargeToken")
                {
                    if (args.Length != 2) return false;
                    byte[] owner = (byte[])args[0];
                    byte[] txid = (byte[])args[1];

                    return rechargeToken(owner, txid);
                }
                if (method == "hasAlreadyCharged")
                {
                    if (args.Length != 1) return false;
                    byte[] txid = (byte[])args[0];

                    return hasAlreadyCharged(txid);
                }
                if (method == "buyDiamond")
                {
                    if (args.Length != 3) return false;
                    byte[] owner = (byte[])args[0];
                    BigInteger sgascount = (BigInteger)args[1];
                    BigInteger diamondcount = (BigInteger)args[2];

                    return buyDiamond(owner, sgascount, diamondcount);
                }
                if (method == "consumeDiamond")
                {
                    if (args.Length != 2) return false;
                    byte[] owner = (byte[])args[0];
                    BigInteger diamondcount = (BigInteger)args[1];

                    return consumeDiamond(owner, diamondcount);
                }
                if (method == "setInfo")
                {
                    if (args.Length != 3) return false;
                    string type = (string)args[0];
                    string roomroundid = (string)args[1];
                    string hash = (string)args[2];

                    return setInfo(type, roomroundid, hash);
                }
                if (method == "getInfo")
                {
                    if (args.Length != 2) return false;
                    string type = (string)args[0];
                    string roomroundid = (string)args[1];

                    return getInfo(type, roomroundid);
                }
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
                    string author = "MJ";
                    string email = "MJ";
                    string description = "MJC";

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
            }
            return false;
        }

    }
}
