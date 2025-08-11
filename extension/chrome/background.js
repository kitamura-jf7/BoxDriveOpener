"use strict";
let manifestData = chrome.runtime.getManifest();
let maninamever = manifestData.short_name + manifestData.version;

var chromebrowser = chrome;
if (typeof browser !== "undefined")
{
    chromebrowser = browser;
}

const yyyymmddhhmmsszzz = new Intl.DateTimeFormat(
  undefined,
  {
    year:   'numeric',
    month:  '2-digit',
    day:    '2-digit',
    hour:   '2-digit',
    minute: '2-digit',
    second: '2-digit',
    fractionalSecondDigits: 3,
  }
)
const hhmmsszzz = new Intl.DateTimeFormat(
  undefined,
  {
    hour:   '2-digit',
    minute: '2-digit',
    second: '2-digit',
    fractionalSecondDigits: 3,
  }
)

///

var dbg =
    0;
    dbg = 1;
//    dbg = 2;
//    dbg = 3;
function dbgPutlog(lv, evt, msg)
{
    if (dbg >= lv)
    {
        console.log(maninamever + " " + yyyymmddhhmmsszzz.format(Date.now()) + "," + evt + "," + msg);
    }
}

var intervaldef = 2000;
var intervalshort = 300;
var interval = 0;

var localUrl = "http://localhost:{port}/";
var localUrlPort = 62678;   //  ポート探索開始ポート番号
var localUrlPortCount = 20; //  ポート探索回数・範囲
var localUrlPortSkip = 20;  //  ポート探索間隔
var localUrlPortNo = -1;    //  接続ポート
var mode_boxlink = false;   //  boxショートカットの作成利用フラグ   true;

async function doBoxURL(purl)
{
    dbgPutlog(2, "doBoxURL", "begin, " + purl);
    try
    {

    for (let i = 0; i < localUrlPortCount; i++)     //  localUrlPortCount回（ポート）までチャレンジ
    {
        fetch((localUrl + "boxurl?url={url}".replaceAll("{url}", purl)).replaceAll("{port}", (localUrlPort + i * localUrlPortSkip).toString()),
        {
            method: "GET"
        })
        .then(function(response)
        {
            let wport = response.url.replace(/^http.*:[/][/].*?:([0-9]*)[/].*$/, "$1");
            localUrlPortNo = parseInt(wport);
            dbgPutlog(1, "doBoxURL", "connected port=" + wport);
        })
        .catch((error) => {})
        ;
    }

    }
    finally
    {
        dbgPutlog(2, "doBoxURL", "end");
    }
}

async function doBoxSupport(purl)
{
    if (localUrlPortNo === -1)
    {
        dbgPutlog(3, "doBoxSupport", "skip, " + purl + "," + localUrlPortNo.toString());
        return; //  skip
    }
    dbgPutlog(2, "doBoxSupport", "begin, " + purl + "," + localUrlPortNo.toString());
    try
    {

    return fetch((localUrl + purl).replaceAll("{port}", localUrlPortNo.toString()),
    {
        method: "GET"
    })
    .then(function(response)
    {
        return response;
    })
    .catch((error) =>
    {
        if (localUrlPortNo !== -1)
        {
            localUrlPortNo = -1;
            dbgPutlog(1, "doBoxSupport", "disconnected");
        }
        return null;
    })
    ;

    }
    finally
    {
        dbgPutlog(2, "doBoxSupport", "end");
    }
}

async function dbgPutlogBoxDrive(lv, evt, msg)
{
    if (localUrlPortNo === -1)
    {
        dbgPutlog(3, "dbgPutlogBoxDrive", "skip, " + lv + "," + evt + "," + msg);
        return; //  skip
    }

    doBoxSupport("dbglog?lv={lv}&evt={evt}&msg={msg}".replaceAll("{lv}", lv).replaceAll("{evt}", encodeURIComponent(evt)).replaceAll("{msg}", encodeURIComponent(msg)))
    .then(function(response)
    {
        return response;
    })
    .catch((error) => {})
    ;
}

async function doBoxSupportRes(purl, callback)
{
    if (localUrlPortNo === -1)
    {
        dbgPutlog(3, "doBoxSupportRes", "skip");
        return; //  skip
    }
    dbgPutlog(2, "doBoxSupportRes", "begin");
    try
    {

    doBoxSupport(purl)
    .then(function(response)
    {
        return response.text();
    })
    .then(function(data)
    {
        dbgPutlog(2, purl, String(data));
        callback(
        {
            Res: String(data)
        });
        return;
    })
    .catch((error) => {})
    ;

    }
    finally
    {
        dbgPutlog(2, "doBoxSupportRes", "end");
    }
}

//

async function dogetBoxDrive(pid, purl, callback)
{
    if (localUrlPortNo === -1)
    {
        dbgPutlog(3, "dogetBoxDrive", "skip, " + pid + ", " + purl);
        return; //  skip
    }
    dbgPutlog(2, "dogetBoxDrive", "begin, " + pid + ", " + purl);
    try
    {

    doBoxSupport("getfolder?id={id}".replaceAll("{id}", pid))
    .then(function(response)
    {
        return response.text();
    })
    .then(function(data)
    {
        dbgPutlog(2, "getBoxDrive", pid + ", " + String(data));
        if ((purl === "") || ((String(data) !== "(none)") && (String(data) !== "(lost)")))
        {
            callback(
            {
                Id: pid
            ,   Path: String(data)
            });
        }
        else
        if (purl !== "")
        {
            chrome.tabs.query({}, tabs =>
            {
                for(let i = 0; i < tabs.length; i++)
                {
                    if (tabs[i].url === purl)
                    {
                        return;
                    }
                }
                chrome.tabs.create({url: purl + "?_doBoxDriveOpen_=0" });
            });
            let proconclose = function(proccnt) {
                doBoxSupport("getfolder?id={id}".replaceAll("{id}", pid))
                .then(function(response)
                {
                    return response.text();
                })
                .then(function(data)
                {
                    dbgPutlog(2, "getBoxDrive", pid + ", " + String(data));
                    if (((String(data) !== "(none)") && (String(data) !== "(lost)")))
                    {
                        callback(
                        {
                            Id: pid
                        ,   Path: String(data)
                        });
                    }
                    else
                    {
                        proccnt++;
                        if (proccnt < 3)
                        {
                            setTimeout(function()
                            {
                                proconclose(proccnt);
                            }
                            , 5000);
                        }
                    }
                })
                ;
            }
            proconclose(0);
        }
        return;
    })
    .catch((error) => {})
    ;

    }
    finally
    {
        dbgPutlog(2, "dogetBoxDrive", "end");
    }
}

async function dosaveBoxDrive(pid, ppath)
{
    if (localUrlPortNo === -1)
    {
        dbgPutlog(3, "dosaveBoxDrive", "skip, " + pid + ", " + ppath);
        return; //  skip
    }
    dbgPutlog(2, "dosaveBoxDrive", "begin, " + pid + ", " + ppath);
    try
    {

    doBoxSupport("folder?id={id}&path={path}".replaceAll("{id}", pid).replaceAll("{path}", encodeURIComponent(ppath)))
    .then(function(response)
    {
        return response;
    })
    .catch((error) => {})
    ;

    }
    finally
    {
        dbgPutlog(2, "dosaveBoxDrive", "end");
    }
}


//XX
function aa()
{
    dbgPutlog(3, "aa", "");
    interval = intervaldef;
    chrome.tabs.query({active: true, lastFocusedWindow: true}, tabs =>
    {
        for(let i = 0; i < tabs.length; i++)
        {
            if ((tabs[i].url.startsWith("about://") === true)
             || (tabs[i].url.startsWith("chrome://") === true)
             || (tabs[i].url.startsWith("edge://") === true)
             || (tabs[i].url.startsWith("extension://") === true))
            {
                continue;
            }

            if (tabs[i].url.match(/https:[/][/].*[.]box[.]com[/](folder|file|collection)[/][0-9a-zA-Z]*([/]|[?]|$)/) != null)   //  box folder/file/collection
            {

            dbgPutlog(3, "tabs[i].url", tabs[i].url);
            var pattern = tabs[i].url.replace(/http.*[/](folder|file)[/]([0-9a-zA-Z]*).*/, "$1")
            var id = tabs[i].url.replace(/http.*[/](folder|file)[/]([0-9a-zA-Z]*).*/, "$2")
            interval = intervalshort;
            dbgPutlog(3, "pattern", pattern);
            dbgPutlog(3, "id", id);

            if (localUrlPortNo === -1)
            {
                doBoxURL(tabs[i].url.replace(/(http.*)[/](folder|file)[/]([0-9a-zA-Z]*).*/, "$1"));
                chromebrowser.tabs.sendMessage(tabs[i].id,
                {
                    method: "ShowOption"
                })
                ;
                let tmp_method = (pattern === "folder" ? "getBoxDrive" : "getBoxFile");
                chromebrowser.tabs.sendMessage(tabs[i].id,
                {
                    method: tmp_method
                ,   Id: id
                ,   Path: ""
                ,   AutoOpen: false
                })
                .then((response) =>
                {
                    dbgPutlog(3, tmp_method, "response=" + response.Res);
                    if ((response.Res === "same") || (response.Res === "save"))
                    {
                        interval = intervaldef;
                    }
                })
                .catch((error) => {})
                ;
            }
            else
            {
                chromebrowser.tabs.sendMessage(tabs[i].id,
                {
                    method: "HideOption"
                })
                ;

                if (tabs[i].url.match(/https:[/][/].*[.]box[.]com[/](folder|file)[/][0-9a-zA-Z]*([/]|[?]|$)/) != null)   //  folder/file
                {
                if ((pattern !== "") && (id !== ""))
                {
                    //if (id != "0")
                    {
                        dogetBoxDrive(id, "", (response) =>
                        {
                            let tmp_method = (pattern === "folder" ? "getBoxDrive" : "getBoxFile");
                            let tmp_Id = response.Id;
                            let tmp_Path = response.Path;
                            let tmp_doopen = false;
                            if(tabs[i].url.indexOf("_doBoxDriveOpen_=") !== -1)
                            {
                                tmp_doopen = true;
                            }
                            dbgPutlog(3, tmp_method, "Id=" + tmp_Id + ", Path=" + tmp_Path);
                            chromebrowser.tabs.sendMessage(tabs[i].id,
                            {
                                method: tmp_method
                            ,   Id: tmp_Id
                            ,   Path: tmp_Path
                            ,   AutoOpen: tmp_doopen
                            })
                            .then((response) =>
                            {
                                dbgPutlog(3, tmp_method, "response=" + response.Res);
                                if ((response.Res === "openfolder"))
                                {
                                    interval = intervalshort;
                                    chromebrowser.tabs.sendMessage(tabs[i].id,
                                    {
                                        method: tmp_method
                                    ,   Id: tmp_Id
                                    ,   Path: tmp_Path
                                    ,   AutoOpen: tmp_doopen
                                    })
                                    .then((response) =>
                                    {
                                        dbgPutlog(3, tmp_method, "response=" + response.Res);
                                        if ((response.Res === "same") || (response.Res === "save"))
                                        {
                                            if(tabs[i].url.indexOf("_doBoxDriveOpen_=1") !== -1)
                                            {
                                                dbgPutlog(3, "open(re)", "url={url}&path={path}".replaceAll("{url}", tabs[i].url.replaceAll("?_doBoxDriveOpen_=1", "").replaceAll("&_doBoxDriveOpen_=1", "")).replaceAll("{path}", ""));
                                                doBoxSupport("open?url={url}&path={path}".replaceAll("{url}", tabs[i].url.replaceAll("?_doBoxDriveOpen_=1", "").replaceAll("&_doBoxDriveOpen_=1", "")).replaceAll("{path}", ""))
                                                .then((response) => {})
                                                .catch((error) => {})
                                                ;
                                            }
                                            else
                                            if(tabs[i].url.indexOf("_doBoxDriveOpen_=2") !== -1)
                                            {
                                                dbgPutlog(3, "copyclipboard(re)", "url={url}&path={path}".replaceAll("{url}", tabs[i].url.replaceAll("?_doBoxDriveOpen_=2", "").replaceAll("&_doBoxDriveOpen_=2", "")).replaceAll("{path}", ""));
                                                doBoxSupport("copyclipboard?url={url}&path={path}".replaceAll("{url}", tabs[i].url.replaceAll("?_doBoxDriveOpen_=2", "").replaceAll("&_doBoxDriveOpen_=2", "")).replaceAll("{path}", ""))
                                                .then((response) => {})
                                                .catch((error) => {})
                                                ;
                                            }
                                            if(tabs[i].url.indexOf("_doBoxDriveOpen_=") !== -1)
                                            {
                                                dbgPutlog(3, "closetab", tabs[i].url);
                                                chromebrowser.tabs.remove(tabs[i].id);
                                            }
                                            interval = intervaldef;
                                        }
                                    })
                                    .catch((error) => {})
                                    ;
                                }
                                if ((response.Res === "same") || (response.Res === "save"))
                                {
                                    if(tabs[i].url.indexOf("_doBoxDriveOpen_=1") !== -1)
                                    {
                                        dbgPutlog(3, "open(re)", "url={url}&path={path}".replaceAll("{url}", tabs[i].url.replaceAll("?_doBoxDriveOpen_=1", "").replaceAll("&_doBoxDriveOpen_=1", "")).replaceAll("{path}", ""));
                                        doBoxSupport("open?url={url}&path={path}".replaceAll("{url}", tabs[i].url.replaceAll("?_doBoxDriveOpen_=1", "").replaceAll("&_doBoxDriveOpen_=1", "")).replaceAll("{path}", ""))
                                        .then((response) => {})
                                        .catch((error) => {})
                                        ;
                                    }
                                    else
                                    if(tabs[i].url.indexOf("_doBoxDriveOpen_=2") !== -1)
                                    {
                                        dbgPutlog(3, "copyclipboard(re)", "url={url}&path={path}".replaceAll("{url}", tabs[i].url.replaceAll("?_doBoxDriveOpen_=2", "").replaceAll("&_doBoxDriveOpen_=2", "")).replaceAll("{path}", ""));
                                        doBoxSupport("copyclipboard?url={url}&path={path}".replaceAll("{url}", tabs[i].url.replaceAll("?_doBoxDriveOpen_=2", "").replaceAll("&_doBoxDriveOpen_=2", "")).replaceAll("{path}", ""))
                                        .then((response) => {})
                                        .catch((error) => {})
                                        ;
                                    }
                                    if(tabs[i].url.indexOf("_doBoxDriveOpen_=") !== -1)
                                    {
                                        dbgPutlog(3, "closetab", tabs[i].url);
                                        chromebrowser.tabs.remove(tabs[i].id);
                                    }
                                    interval = intervaldef;
                                }
                            })
                            .catch((error) => {})
                            ;
                        });
                    }
                }
    
                }
//                  else
//                  if (tabs[i].url.match(/https:[/][/].*[.]box[.]com[/](collection)[/][0-9a-zA-Z]*([/]|[?]|$)/) != null)   //  collection
//                  {
//                      dbgPutlog(3, "getBoxCollection", tabs[i].url);
//                      chromebrowser.tabs.sendMessage(tabs[i].id,
//                      {
//                          method: "getBoxCollection"
//                      })
//                      ;
//                  }
                }

            }
        }
    });
    if (interval !== 0)
    {
        setTimeout(aa, interval);
    }
}


{
    dbgPutlog(1, "begin", "background");
    interval = intervaldef;
    aa();
}

//

//タブ更新時に実行
chrome.tabs.onUpdated.addListener(function(id, info, tab)
{
    if (tab.id === undefined)
    {
    }
    else
    if (info.status === "loading")
    {
        dbgPutlog(3, "onUpdated, loading", tab.url);
        chromebrowser.tabs.sendMessage(tab.id,
        {
            method: "onUpdated, loading"
        ,   Url: tab.url
        })
        .then((response) => {})
        .catch((error) => {})
        ;
    }
    else
    if (info.status === "complete")
    {
        dbgPutlog(3, "onUpdated, complete", tab.url);
        chromebrowser.tabs.sendMessage(tab.id,
        {
            method: "onUpdated, complete"
        ,   Url: tab.url
        })
        .then((response) => {})
        .catch((error) => {})
        ;
    }
});

chrome.runtime.onMessage.addListener(function(request, sender, callback)
{
    dbgPutlog(3, "onMessage(B)", request.message);
    if (request.message === "dbgPutlog")
    {
        dbgPutlog(request.Level, "(C)" + request.Event, request.Msg);
        return true;
    }
    else
    if (request.message === "dbgPutlogBoxDrive")
    {
        dbgPutlogBoxDrive(request.Level, "(C)" + request.Event, request.Msg);
        return true;
    }
    else
    if (request.message === "openOption")
    {
        //  オプションページを開く
        chrome.runtime.openOptionsPage();
        return true;
    }
    else
    if (request.message === "waittimer")
    {
        setTimeout(function()
        {
            callback(
            {
                Res: "ok"
            });
            return;
        }
        , request.Interval);
        return true;
    }
    else
    if (request.message === "checkDebugLevel")
    {
        doBoxSupport("checkdbglevel")
        .then(function(response)
        {
            return response.text();
        })
        .then(function(data)
        {
            let wdbg = parseInt(String(data).slice("ok,".length));
            if (wdbg !== dbg)
            {
                dbg = wdbg;
                dbgPutlog(1, "DebugLevel", dbg);
            }
            callback(
            {
                Level: dbg
            });
            return;
        })
        .catch((error) => {})
        ;
        return true;
    }
    else
    if (request.message === "saveBoxURL")
    {
        doBoxURL(request.Url);
        return true;
    }
    else
    if (request.message === "getBoxDrive")
    {
        dogetBoxDrive(request.Id, request.Url, callback);
        return true;
    }
    else
    if (request.message === "saveBoxDrive")
    {
        dosaveBoxDrive(request.Id, request.Path);
        return true;
    }
    else
    if (request.message === "copyBoxDrive")
    {
        doBoxSupport("copyclipboard?url={url}&path={path}".replaceAll("{url}", request.Url).replaceAll("{path}", encodeURIComponent(request.Path)))
        .then((response) => {})
        .catch((error) => {})
        ;
        return true;
    }
    else
    if (request.message === "openBoxDrive")
    {
        doBoxSupport("open?url={url}&path={path}".replaceAll("{url}", request.Url).replaceAll("{path}", encodeURIComponent(request.Path)))
        .then((response) => {})
        .catch((error) => {})
        ;
        return true;
    }
    else
    if (request.message === "openBoxBrowser")
    {
        doBoxSupport("open2?url={url}".replaceAll("{url}", request.Url))
        .then((response) => {})
        .catch((error) => {})
        ;
        return true;
    }
    else
    if (request.message === "fireMouseMove")
    {
        doBoxSupport("firemousemove");
        return true;
    }
    else
    if (request.message === "checkOption")
    {
        doBoxSupportRes("checkoption", callback);
        return true;
    }
    else
    if (request.message === "makelinkBoxDrive")
    {
        doBoxSupport("makelink?id={id}&url={url}&path={path}".replaceAll("{id}", request.Id).replaceAll("{url}", request.Url).replaceAll("{path}", encodeURIComponent(request.Path)))
        .then((response) => {})
        .catch((error) => {})
        ;
        return true;
    }
    else
    if (request.message === "getTimestamp")
    {
        doBoxSupportRes("timestamp?path={path}".replaceAll("{path}", encodeURIComponent(request.Path)), callback);
        return true;
    }
    else
    if (request.message === "getData")
    {
        doBoxSupportRes("data?path={path}".replaceAll("{path}", encodeURIComponent(request.Path)), callback);
        return true;
    }
    else
    if (request.message === "fetchfolder")
    {
        //  未使用
        fetch(request.Url)
        .then(res => res.text())
        .then(text => {
            dbgPutlog(3, request.message + " text", text);
            callback({ text: text })
        })
        ;
        return true;
    }
    return;
});

