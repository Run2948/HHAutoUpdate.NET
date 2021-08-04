[TOC]

# 如何让 Node.js 应用运行在 Windows 系统 IIS 中

最近比较喜欢用Node.js做一些简单的应用，一直想要部署到生产环境中，但是手上只有一台windows server 2008服务器，并且已经开启了IIS服务，运行了很多.Net开发的网站，80端口已经被占用了。  

起初是想用nginx来作为web服务器监听80端口，将所有web访问转发到对应的IIS和node，但由于已运行的老站点众多，如此配置实在需要大量的精力，于是突发奇想，能不能直接利用IIS来托管node服务呢？进过一番搜索之后发现了iisnode模块，可以很轻松的解决这个问题。下面就把实操步骤分享出来，方便有同样需求的朋友参考。

## 情况说明

1. 现有的服务器环境只有 Windows Server  服务器 (具体为 Windows Server 2019 Datacenter)
2. 服务器已经启用了 IIS 服务并且部署了很多 .Net / .Net Core 站点，80 端口毫无疑问被占用

## 尝试方案

###  1.  Nginx 监听非80端口的转发

```nginx
server {
     listen       81;  # 监听 81 端口做转发
     server_name  localhost test.itduck.cn;
 
     location / {
      proxy_pass  http://localhost:9000;
      proxy_set_header   Host             $host:$server_port; 
      proxy_set_header   X-Real-IP        $remote_addr;
      proxy_set_header   X-Forwarded-For  $proxy_add_x_forwarded_for;
      proxy_set_header   Via  "nginx";
     }
 }
```

**注意特殊情况: ** 如果服务器的 81 端口没有对外开放访问，而是仅仅开放了一个 6359 端口来代理内网 81 端口。这时 nginx 的配置需要做一下调整：

```nginx
server {
     listen       81;  # 监听 81 端口做转发
     server_name  localhost test.itduck.cn;
 
     location / {
      proxy_pass  http://localhost:9000;
      proxy_set_header   Host             $host:6359; # 直接指向代理 81 端口的端口，这里是 6359
      proxy_set_header   X-Real-IP        $remote_addr;
      proxy_set_header   X-Forwarded-For  $proxy_add_x_forwarded_for;
      proxy_set_header   Via  "nginx";
     }
 }
```

## 环境准备


### 1. 安装 IIS Node 模块

首先**iisnode** 是一个**IIS Module**加载到IIS以后，就可以在任意站点中通过 Web.config 指定某些路径转交给node程序执行，通过参数配置，可以设置启动的node进程个数，以及最大连接数等。并且可以监听站点文件变化，自动重启node服务等功能。

**iisnode** 代码是用 C++ 编写的，托管在github上。如果不想自己编译，也可以直接下载适合自己的版本。

https://github.com/tjanczuk/iisnode/wiki/iisnode-releases

如果你的服务器是 Windows Server 2008 x64 系统，选择下载 "iisnode for iis 7/8 (x64)" 安装程序，只要版本正确，安装过程并没有需要特别注意的，自己根据提示一步一步完成即可。

而我的服务器是 Windows Server 2019 x64 系统，IIS 的版本已经是 10.x  了，从原 GitHub 仓库找不到适合我的安装程序版本。好在最后是苦心人天不负，发现 [Microsoft Azure](https://github.com/Azure) 团队已经 fork 了原作者的代码仓库，并且发布了最新的版本 [Azure/iisnode](https://github.com/Azure/iisnode/releases/download/v0.2.26/iisnode-full-v0.2.26-x64.msi) 来兼容 IIS 10。 参见：[iisnode安装问题解决](https://www.cnblogs.com/CoderMonkie/p/deploy-nodejs-server-to-iis-use-iisnode.html)


### 2. 安装 URL Rewrite 模块

安装一下 IIS 的 URL Rewrite 模块（需要利用 rewrite 功能转发相关的请求交给node服务来执行），如果 IIS 中已经有了 URL Rewrite 模块 （貌似有些版本的 IIS 安装后就有了 URL Rewrite 模块），则可以略过这一步。

如果IIS上默认有安装Web平台安装程序，我们可以使用平台自动安装URL Rewrite重写工具，打开IIS(Internet 信息服务管理器)，在管理器主页中找到管理项，打开Web平台安装程序，如下图：

![](http://shiyousan.com/UserFiles/images/2015/04/635646254870261696/09_635646254870261696.jpg)



在列表中找到URL重写工具，点击添加后点击安装，即可自动安装。如下图：

![](http://shiyousan.com/UserFiles/images/2015/04/635646254870261696/10_635646254870261696.jpg)



除此之外，我们也可以手动下载 URL Rewrite 插件，这是官方地址：[URL Rewrite下载](http://www.iis.net/downloads/microsoft/url-rewrite)

![](http://shiyousan.com/UserFiles/images/2015/04/635646254870261696/12_635646254870261696.jpg)

 

网路情况不佳的时候，还可以下载离线安装包，[URL Rewrite Module 2.1](https://www.iis.net/downloads/microsoft/url-rewrite#additionalDownloads)

![](http://shiyousan.com/UserFiles/images/2015/04/635646254870261696/02_635646254870261696.JPG)



## 开始部署

### 1.  部署基础 Node.js 应用

**提示：部署 Node.js 应用的流程和部署 .Net/ .Net Core 应用的流程完全一致。** 在 IIS 中新建网站，新建默认程序应用池，将目录指定到 nodejs 应用目录，配置端口和域名，最后点击立即启动。

**关键是最后的这一步**  在应用根目录下新建 `web.config` 配置文件并写入如下的内容：

```xml
<configuration>
    <system.webServer>
        <handlers>
            <add name="iisnode" path="app.js" verb="*" modules="iisnode" resourceType="Unspecified" requireAccess="Script" />
        </handlers>
 
        <rewrite>
            <rules>
                <rule name="all">
                    <match url="/*" />
                    <action type="Rewrite" url="app.js" />
                </rule>
            </rules>
        </rewrite>
 
        <iisnode promoteServerVars="REMOTE_ADDR" />
    </system.webServer>
</configuration>
```

作用是将当前目录的所有请求都利用 `iisnode` 模块转发到 `node` 服务，并指定了 node 的`执行目录`。其中的 `app.js` 是node应用的`入口文件`（可以按照自己的目录结构进行修改）。上述步骤操作完毕后，一个基本的 Node.js 应用就成功部署在了 IIS 服务上，打开浏览器访问网站就可以看到效果了。



**如果运行的时候出现如下错误：**


> 500.19 配置错误 不能在此路径中使用此配置节。如果在父级别上锁定了该节，便会出现这种情况。锁定是默认设置的 ( overrideModeDefault = `"Deny"`)，或者是通过包含 overrideMode = `"Deny"` 或旧有的 allowOverride = `"false"` 的位置标记明确设置的。


通过 cmd 运行如下代码即可解决（其中的 `handlers` 是报错的节点名字）：

```cmd
%windir%\system32\inetsrv\appcmd unlock config -section:system.webServer/handlers
```



### 2.  部署基于 Express 的项目

* 1. 新建程序入口文件 `launch.js` 代码如下：

```javascript
#!/usr/bin/env node

require('./bin/www');
```

如果是基于 Express 3.x 创建的项目，或者原程序中已经有入口文件如 `app.js` `index.js` `main.js` 等等，则可以略过这一步。

* 2. 将 `web.config`  配置文件的文件改为如下内容

```xml
<configuration>
    <system.webServer>
        <handlers>
            <add name="iisnode" path="launch.js" verb="*" modules="iisnode" resourceType="Unspecified" requireAccess="Script" />
        </handlers>

        <rewrite>
            <rules>
                <rule name="all">
                    <match url="/*" />
                    <action type="Rewrite" url="launch.js" />
                </rule>
            </rules>
        </rewrite>

        <iisnode promoteServerVars="REMOTE_ADDR" />
    </system.webServer>
</configuration>
```



**如果出现如下错误：**

> The iisnode module is unable to start the node.exe process. Make sure the node.exe executable is available at the location specified in the system.webServer/iisnode/@nodeProcessCommandLine element of web.config. By default node.exe is expected in one of the directories listed in the PATH environment variable

在 `web.config` 中的 `system.webServer` 节点加上以下内容即可解决：

```xml
<iisnode watchedFiles="*.js;node_modules\*;routes\*.js;views\*.jade"  nodeProcessCommandLine="C:\Program Files\nodejs\node.exe"/>
```



最后附上我本次在 `Windows Server 2019 x64` +  `IIS 10.x `中部署 Node.js 应用的示例：

```xml
<configuration>
    <system.webServer>
        <handlers>
            <add name="iisnode" path="launch.js" verb="*" modules="iisnode" resourceType="Unspecified" requireAccess="Script" />
        </handlers>

        <rewrite>
            <rules>
                <rule name="all">
                    <match url="/*" />
                    <action type="Rewrite" url="launch.js" />
                </rule>
            </rules>
        </rewrite>
        
        <!-- nodeProcessCommandLine 节点的值是您本机 node 的安装路径 -->
        <iisnode promoteServerVars="REMOTE_ADDR" watchedFiles="*.js;node_modules\*;routes\*.js;views\*.jade"  nodeProcessCommandLine="C:\Program Files\nodejs\node.exe"/>
    </system.webServer>
</configuration>
```



## 特别鸣谢

[aieceo](https://www.cnblogs.com/aieceo/) - [利用iisnode模块，让你的Node.js应用跑在Windows系统IIS中](https://www.cnblogs.com/aieceo/p/7906640.html)

[Coder-Monkie](https://cloud.tencent.com/developer/user/4431576) -  [iis10是不是没有对应的iisnode，安装了一下午不成功？](https://cloud.tencent.com/developer/ask/191822)

[mofy](https://www.cnblogs.com/z-books/) - [nginx使用非80端口时url带端口号的解决办法](https://www.cnblogs.com/z-books/p/12410979.html)
