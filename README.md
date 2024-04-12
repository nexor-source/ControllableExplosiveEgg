# Controllable Explosive Egg

## (Functions) 功能

- This mod can make your easter eggs even better (i.e. G keys don't explode when dropped, left keys do explode) (only works on yourself)

  这个mod可以让你的彩蛋变得更好 (即G键丢下不爆炸，左键丢出必爆炸) (仅对自己有效)

- This mod can allow you to modify the explosion probability of overall easter eggs (server only).

  这个mod可以允许你在配置文件中修改全局彩蛋的爆炸概率 (仅服务端时生效)

- This mod can also predict whether the Easter egg in your hand will explode or not, provided you don't turn on the Better Easter Egg mode (only works on yourself).

  这个mod还可以预测手中的复活节彩蛋是否会爆炸，前提是你没有开启更好的彩蛋模式 (仅对自己有效)

## (How Easter eggs explode) 彩蛋是如何爆炸的

Whenever the Easter egg is picked up or switched to, the game passes an event (`EquipItem`) to the server to pick up the egg. The server then randomly calculates whether the egg will explode based on the current player's location and the map's seed, and sends information on whether it will explode to all clients.

每当彩蛋被拿到手上时（不论是捡起还是被切换到），游戏就会传递给服务器一个拿起彩蛋的事件(`EquipItem`)，服务器则会根据当前玩家位置和地图随机种子计算出这个彩蛋是否会发生爆炸，并且将是否会爆炸的信息发送给所有客户端。
