# 808Test
808服务器压力测试工具，压测808服务器，发送位置0x0200消息，使用前记得修改配置文件。
    <!--服务器IP和端口(113.31.92.200:8201)-->
    <add key="remoteServerPort" value="10.1.97.12:32195"/>
    <!--模拟终端id区间（设置200000-200100，将自动生成010000200000-010000200100这样101个设备）-->
    <add key="deviceIdRange" value="200000-200010"/>
    <!--每个消息间隔（秒）-->
    <add key="interval" value="1"/>
    <!--每个设备发送共多少个包(默认0x0200)后断开连接-->
    <add key="sendCountByOneDevice" value="10"/>
    <!--控制台是否打印发送日志（消耗大量cpu）true or false-->
    <add key="sendLogPrint" value="false"/>
    <!--消息是否等待服务端同步应答（如果开启，服务器来不及回复应答的时候，控制台看上去就像假死了）true or false-->
    <add key="waitReceive" value="false"/>
    <!--控制台是否打印接收日志（消耗大量cpu）,该开关只有开启了服务端应答之后才有效 true or false-->
    <add key="receiveLogPrint" value="false"/>

原始源码来自于互联网搜索来的无名氏，修改了bug和做了一些优化。

本着来自哪里，依然去向哪里！
