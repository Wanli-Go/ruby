1. 首先部署服务器端：运行 `(在代码目录下) gcc ./backend/server.c` 然后 `./backend/a.out`. 应该可以显示图中的结果：

![img](file:///C:/Users/xingw/AppData/Local/Temp/msohtmlclip1/01/clip_image002.jpg)

2. 

|      |                                                              |      |                                                              |
| ---- | ------------------------------------------------------------ | ---- | ------------------------------------------------------------ |
|      | ![img](file:///C:/Users/xingw/AppData/Local/Temp/msohtmlclip1/01/clip_image005.jpg) |      | ![img](file:///C:/Users/xingw/AppData/Local/Temp/msohtmlclip1/01/clip_image006.jpg) |
|      |                                                              |      |                                                              |

然后，在代码目录下的frontend/builds/ 文件夹下，用Windows打开LinuxFrontend;



应该可以看到出现了一个游戏窗口；第一个游戏窗口必须选择“Play As Ruby”。因为在后端的逻辑中默认了这一点（原谅我没有时间来保证它的鲁棒性）。此时窗口中应该出现一个Ruby：

![img](file:///C:/Users/xingw/AppData/Local/Temp/msohtmlclip1/01/clip_image008.jpg)

如果没有出现，说明与服务器的连接不成功。Unity的代码存储在 /frontend/Assets/Scripts文件夹下，其中NetworkClient.cs代码中定义了与服务器链接的地址和端口号。默认情况下，这个地址是**127.0.0.1**（即localhost），端口为**10035**（后端的端口号也是10035）。

3. 还是在代码目录下的frontend/builds/ 文件夹下，用Windows打开 LinuxFrontend，运行另一个游戏实例。由于游戏功能不完善，现在只能选择“Play As Robots”。如果第二步运行成功，在这一步应该能看见摄像机放缩到了整个地图的视野，并且能看到另一个玩家操控的Ruby的位置。不久，就可以看到3个机器人从地图的边缘向地图中间行走。

![img](file:///C:/Users/xingw/AppData/Local/Temp/msohtmlclip1/01/clip_image010.jpg)

4. 操控两个玩家：

在Ruby玩家端（第一个窗口）按下WASD可以控制她的走向。按下鼠标左键可以朝她所在的方向发射子弹（虽然在Robots玩家端不可见）。Ruby玩家的目标在血量耗尽之前是击败所有机器人。

在Robots玩家端，当在地图中单击某个点的时候，所有僵尸都会向那个点行走。基于这个来操控机器人并尝试清空Ruby的血量来取得胜利。

演示视频中演示了两段视频，分别展示Ruby玩家获胜（Robots玩家失败）或Robots玩家获胜（Ruby玩家失败）的情况。

 

### 协议设计

前端和服务器端的消息传递采用了非常简单的方式：当一个玩家或僵尸移动时，它的坐标会不断地发送给服务器，而服务器会把这个坐标对其它玩家进行转发（虽然完整功能只支持两个玩家进行游玩，但是在连接多个Ruby玩家的时候，他们的位置也可以同步）。其它的消息还包括角色选择消息、修复机器人消息、血量改变消息。

我知道这种消息传递的方式容易出问题，因为前端每渲染一帧，都会向后端服务器发送一个新的位置信息。在把Robots玩家可以操纵的僵尸数量调整成大于4个以上时，就会出现明显的同步延迟。在发现这个问题时已经比较晚了，不过我确实意识到了开发游戏服务器端固定的Tick Rate的有效性。

**1.**   **角色选择消息：**

\-    前端到后端传送角色选择消息：

n Message Type (字节，1 byte) – 0x01

n Message Length (int，4 bytes)

n Selected Role (字符串，variable length) – “Ruby” 或 “Zombies”

\-    后端确认角色选择消息，传回一个ID：

n Message Type (字节，1 byte) – 0x01

n Message Length (int，4 bytes)

n Player Id (int, 4 bytes)

n Selected Role (字符串，variable length) – “Ruby” 或 “Zombies”

**2.**   **角色/****僵尸位置更新消息**

\-    前端玩家位置更新向后端传送位置数据

n Message Type (字节，1 byte) – 0x02

n Message Length (int，4 bytes)

n Player Id (int, 4 bytes) // 位置更新的角色/僵尸的ID

n X Position (float, 4 bytes)

n Y Position (float, 4 bytes)

\-    后端把该玩家/僵尸的新位置转发给其它玩家

n Message Type (字节，1 byte) – 0x02

n Player Id (int, 4 bytes) // 位置更新的角色/僵尸的ID

n X Position (float, 4 bytes)

n Y Position (float, 4 bytes)

**3.**   **僵尸修复消息（当Ruby****玩家端打倒一个僵尸的时候，会把这个僵尸的ID****传回服务器，发送给Robots****玩家（和其它玩家，但是目前不支持多玩家））**

\-    前端发送被击倒的僵尸的ID

n Message Type (字节，1 byte) – 0x03

n Message Length (int，4 bytes)

n Zombie Id (int, 4 bytes) // 被击倒的僵尸的ID

\-    后端传递被击倒的僵尸的ID

n Message Type (字节，1 byte) – 0x03

n Zombie Id (int, 4 bytes) // 被击倒的僵尸的ID

**4.**   **血量消息（当Ruby****玩家端碰到僵尸或者地刺的时候血量会减一，并把新的血量传送给服务器，再由服务器发送给Robots****玩家（和其它玩家，但是目前不支持多玩家））**

\-    前端发送Ruby玩家新的血量

n Message Type (字节，1 byte) – 0x04

n Message Length (int，4 bytes)

n Player Id (int, 4 bytes) // 血量改变的角色/僵尸的ID

n Health (int, 4 bytes) // 新的血量

\-    后端传递这个Ruby玩家新的血量到其他玩家

n Message Type (字节，1 byte) – 0x04

n Player Id (int, 4 bytes) // 血量改变的角色/僵尸的ID

n Health (int, 4 bytes) // 新的血量

### 后端代码

后端代码是基于实验三编写Sockets服务器的代码进一步实施的。除了epoll需要的hdnc()来处理新链接(handle new connection)和hdnd()来处理数据(handle new data)之外，对于协议中中每一种类型的消息，都定义了不同的处理函数，比如hdfixed()：

![img](file:///C:/Users/xingw/AppData/Local/Temp/msohtmlclip1/01/clip_image012.jpg)