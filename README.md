# AxibugRedirector

重定向任何Windows应用程序域名解析到指定IP的工具

### 有什么用？

您可以知道任何Windows程序/游戏，访问了什么域名

您可以通过配置让某个Windows程序/游戏，指定的域名定向到你的服务器

比如用于私服研究、用于私服登录器。或将某程序的请求指向到自己服务器

或者希望重定向但是又不希望更改系统HOST等需求之用。

### 原理

核心原理是Hook Win32的WS2_32.DLL 的gethostname等函数，指向您配置的IP

【控制台版本使用方法】

下载Release版本，或者自编译AxibugRedirector项目.
编辑可执行程序目录的config.cfg配置文件

格式（可多行）
本地监听端口:目标IP:目标端口

如：

	baidu.com:1.2.3.4
	google.com:127.0.0.1

表示

将指定进程访问baidu.com的IP解析结果改为1.2.3.4，达到baidu.com访问1.2.3.4的效果

将指定进程访问google.com的IP解析结果改为本地，达到google.com访问本地的效果