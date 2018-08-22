# 《天堂岛麻将》

是一款基于区块链技术的棋牌游戏，在游戏内可使用Gas转帐、购买游戏资产并贩售钻石再依比例获取Gas。有别于传统棋牌，牌局开始时，服务器将乱数产生的牌组信息透过Hash发布至链上，是真正公正无法窜改的麻将游戏，游戏数据打牌过程、结束牌局等信息，资产交易上链，保护玩家资产安全。系统承载量可同时10万人上线，使用大型游戏的分布式架构、微服务架构及消息通信对列中间件，同时满足性能和可靠性。

## MJ合约

    (1)用于发行游戏资产MJC(钻石)，有关资产交易全部上链。

    (2)游戏逻辑节合行为数据上链储存。
	
## 系统架构图


## 技术概要

    (1) 采用 mqtt
	* 不仅限于手机 app, 可在 web 上搭配 websocket 使用 mqtt 协议
	* 基于订阅/发布的消息转发服务
	* 提供三个层次的 QoS 设定, 可确保至少送达一次

    (2) 基于 redis 储存 & 消息转送
	* 提供游戏资料储存
	* 提供消息转送历史
	* 标记消息转送成功

    (3) 消息通送服务 (微服务)
	* 同时支援 push / pull 方式接收消息
	* 转发消息至 EMQ
	* 定期清除消息历史

    (4) EMQ 消息对列
	* 基于出色的软实时、低延时、高并发、分布式的Erlang/OTP语言平台开发设计，支持百万级连接和分布式集群
	* 完整支持MQTT V3.1/V3.1.1协议规范，扩展支持WebSocket、Stomp、CoAP、MQTT-SN或私有TCP协议
	* 按主题树(Topic Trie)和路由表(Routing Table)发布订阅模式在集群节点间转发路由MQTT消息

