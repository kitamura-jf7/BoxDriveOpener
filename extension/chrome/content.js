"use strict";

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

var dbg =
    0;
    dbg = 1;
//    dbg = 2;
function dbgPutlog(lv, evt, msg)
{
    if (dbg >= lv)
    {
        console.log(yyyymmddhhmmsszzz.format(Date.now()) + "," + evt + "," + msg);
    }
    try
    {
        chrome.runtime.sendMessage(
        {
            message: "dbgPutlog"
        ,   Level: lv
        ,   Event: evt
        ,   Msg: String(msg)
        })
        .then((response) => {})
        .catch((error) => {})
        ;
    }
    catch (error)
    {
        console.error("error=" + error);
    }
}

function dbgPutlogBoxDrive(lv, evt, msg)
{
    dbgPutlog(lv, evt, msg);
    try
    {
        chrome.runtime.sendMessage(
        {
            message: "dbgPutlogBoxDrive"
        ,   Level: lv
        ,   Event: evt
        ,   Msg: String(msg)
        })
        .then((response) => {})
        .catch((error) => {})
        ;
    }
    catch (error)
    {
        console.error("error=" + error);
    }
}

function str2bool(boolstr)
{
    if (boolstr === "")
    {
        return null;
    }
    if (boolstr.toLowerCase() === "null")
    {
        return null;
    }
    if (boolstr.toLowerCase() === "true")
    {
        return true;
    }
    else
    {
        return false;
    }
}


var varelem = null;
var mode_opener = false;
var mode_boxlink = null;
var mode_slidebar_folder = false;   //  初期状態で、true=開く、false=閉じる
var mode_slidebar_file = true;      //  初期状態で、true=開く、false=閉じる

const BG_NA_COLOR = "#fffff0";
const BG_A_COLOR = BG_NA_COLOR;
const BG_CLICK_COLOR = "#f5deb3";
const FG_NA_COLOR = "silver";
const FG_A_COLOR = "red";

function setbuttonevent(pelement, pmessage, pid, purl, ppath)
{
    pelement.disabled = true;
    pelement.style.backgroundColor = BG_NA_COLOR;
    pelement.style.color = FG_NA_COLOR;
    pelement.style.border = "thin";   //     solid   #808080
    pelement.style.fontSize = "70%";
    pelement.tabIndex = "-1";
    pelement.addEventListener("click", (event) =>
    {
        //  click時処理
        dbgPutlogBoxDrive(3, "click", pmessage + "," + pid + "," + purl + "," + ppath);
        event.preventDefault();     //  親のclickイベントを抑止
        event.target.style.backgroundColor = BG_CLICK_COLOR;
        clickmain(event.target, function()
        {
            event.target.style.backgroundColor = BG_NA_COLOR;
        });
    });
    pelement.addEventListener("mouseover", (event) =>
    {
        //  mouseover時処理
        event.target.style.color = FG_A_COLOR;
        event.target.style.backgroundColor = BG_A_COLOR;
    });
    pelement.addEventListener("mouseleave", (event) =>
    {
        //  mouseleave時処理
        event.target.style.color = FG_NA_COLOR;
        event.target.style.backgroundColor = BG_NA_COLOR;
    });
}

const BTNCC_TEXT = "CC";
const BTNCC_HINT = "";  //"boxDriveパスをクリップボードにコピー";
const BTNOPEN_TEXT = ">>>";
const BTNOPEN_HINT = "";  //"boxDriveを開く";
const BTNMKSC_TEXT = "Create boxSC";     //  boxショートカットの作成
const BTNMKSC_HINT = "";  //"同じフォルダにboxショートカットを作成";
const BTNSCOPEN_TEXT = "link" + BTNOPEN_TEXT;
const BTNSCOPEN_HINT = "";  //"リンク先のboxDriveを開く";
const BTNSCLINKOPEN_TEXT = "link" + BTNOPEN_TEXT + "box";   //  リンク先をboxで開く
const BTNSCLINKOPEN_HINT = "";  //"リンク先のboxURLを開く";
const BTNOPTION_TEXT = "BoxDrive Support Extension オプションページを開く";
const BTNOPTION_HINT = "";  //"オプションページを開く";


function addupdbutton(pelement, pclass, psubclass, ptext, phint, pmessage, pid, purl, ppath)
{
    if (pelement === null)
    {
        return;
    }

    var tmpelm = pelement.querySelector("." + pclass);
    if (tmpelm === null)
    {
        tmpelm = document.createElement("button");
        tmpelm.type = "button";
        tmpelm.className = pclass + (" " + psubclass).trimEnd();
        pelement.appendChild(tmpelm);
        tmpelm.textContent = ptext;
        tmpelm.ariaLabel = phint;
        tmpelm.setAttribute("xxbdmessage", pmessage);
        tmpelm.setAttribute("xxbdid", pid);
        tmpelm.setAttribute("xxbdurl", purl);
        tmpelm.setAttribute("xxbdpath", ppath);
        setbuttonevent(tmpelm, pmessage, pid, purl, ppath);
    }
    if (tmpelm.getAttribute("xxbdid") !== pid)
    {
        tmpelm.setAttribute("xxbdid", pid);
        tmpelm.setAttribute("xxbdurl", purl);
        tmpelm.setAttribute("xxbdpath", ppath);
    }
    else
    if (tmpelm.getAttribute("xxbdurl") !== purl)
    {
        tmpelm.setAttribute("xxbdurl", purl);
        tmpelm.setAttribute("xxbdpath", ppath);
    }
    else
    if (tmpelm.getAttribute("xxbdpath") !== ppath)
    {
        tmpelm.setAttribute("xxbdpath", ppath);
    }
    return tmpelm;
}

function clickmain(ptarget, callback)
{
    var pmessage = ptarget.getAttribute("xxbdmessage");
    var pid = ptarget.getAttribute("xxbdid");
    var purl = ptarget.getAttribute("xxbdurl");
    var ppath = ptarget.getAttribute("xxbdpath");
    if (mode_opener !== true)
    {
        dbgPutlog(3, "clickmain00", "target," + pmessage + "," + pid + "," + purl + "," + ppath);
        if (pmessage === "copyBoxDrive")
        {
            //  クリップボードにコピー（Openerなし版）
            let path = "";
            if ([...document.querySelectorAll(".ItemListBreadcrumb-listItem")].map((v) => v.innerText).filter((v) => v !== "" && v == "すべてのファイル") == "すべてのファイル")
            {
                //  すべてのファイルで始まっている
                dbgPutlog(3, "boxfolder", "folder/0");
            }
            else
            {
                //  フォルダ階層取得
                const dotButton = document.querySelectorAll(".ItemListBreadcrumb > button")[0];
                if (dotButton)
                {
                    let dotButton2 = document.querySelectorAll("a[data-resin-target='openfolder'].menu-item")[0];
                    if (dotButton2)
                    {
                        path += [ ...document.querySelectorAll("a[data-resin-target='openfolder'].menu-item"), ].map((e) => e.innerText).filter((v) => v !== "すべてのファイル").join("/");
                        if (!path.endsWith("/")) path += "/";
                        dotButton.click();
                    }
                    else
                    {
                        dbgPutlog(3, "boxfolder", "openfolder");
                        dotButton.click();  //  開く
                        chrome.runtime.sendMessage(
                        {
                            message: "waittimer"
                        ,   Interval: 300
                        })
                        .then((response2) =>
                        {
                            if (response2.Res === "ok")
                            {
                                clickmain(ptarget, callback);   //  再帰
                            }
                        })
                        ;
                        return "click";
                    }
                }
            }
            dbgPutlog(2, "boxfolder", "path=" + path);
            path += ppath;
            if (path.startsWith("/") === false) path = "/" + path;
            path = "%USERPROFILE%/Box" + path;

            navigator.clipboard.writeText(path)
            .then(() =>
            {
                chrome.runtime.sendMessage(
                {
                    message: "waittimer"
                ,   Interval: 200
                })
                .then((response2) =>
                {
                    if (response2.Res === "ok")
                    {
                        callback("ok");
                    }
                })
                ;
            })
            ;
            return "do";
        }
        else
        {
            //  実行
            dbgPutlog(3, "clickmain", "do," + pmessage + "," + pid + "," + purl + "," + ppath);
            chrome.runtime.sendMessage(
            {
                message: pmessage
            ,   Id: pid
            ,   Url: purl
            ,   Path: ppath
            })
            .then((response) =>
            {
                chrome.runtime.sendMessage(
                {
                    message: "waittimer"
                ,   Interval: 200
                })
                .then((response2) =>
                {
                    if (response2.Res === "ok")
                    {
                        callback(response);
                    }
                })
                ;
            })
            ;
        }
    }
    else
    {
        dbgPutlogBoxDrive(3, "clickmain00", "target," + pmessage + "," + pid + "," + purl + "," + ppath);

        if (ppath.match(/^.*%([0-9]*)%[/].*$/) != null)
        {
            let wid = ppath.replace(/^.*%([0-9]*)%[/].*$/, "$1");
            dbgPutlogBoxDrive(3, "clickmain", "getBoxDrive," + wid);
            chrome.runtime.sendMessage(
            {
                message: "getBoxDrive"
            ,   Id: wid
            ,   Url: window.location.protocol + "//" + window.location.host + "/folder/" + wid
            })
            .then((response) =>
            {
                dbgPutlogBoxDrive(3, "boxfile", "getBoxDrive," + response.Id + "," + response.Path);
                if ((response.Path === "(none)")
                 || (response.Path === "(lost)"))
                {
                    //  取得できなかった!?
                    return "none";
                }
                else
                {
                    //  取得した情報で実行
                    let wpath = ppath.replaceAll("%" + response.Id + "%", response.Path);
                    dbgPutlogBoxDrive(3, "clickmain", "do," + pmessage + "," + pid + "," + purl + "," + wpath);
                    chrome.runtime.sendMessage(
                    {
                        message: pmessage
                    ,   Id: pid
                    ,   Url: purl
                    ,   Path: wpath
                    })
                    .then((response) =>
                    {
                        chrome.runtime.sendMessage(
                        {
                            message: "waittimer"
                        ,   Interval: 200
                        })
                        .then((response2) =>
                        {
                            if (response2.Res === "ok")
                            {
                                callback(response);
                            }
                        })
                        ;
                    })
                    ;
                    return "do";
                }
            })
            ;
        }
        else
        {
            //  実行
            dbgPutlogBoxDrive(3, "clickmain", "do," + pmessage + "," + pid + "," + purl + "," + ppath);
            chrome.runtime.sendMessage(
            {
                message: pmessage
            ,   Id: pid
            ,   Url: purl
            ,   Path: ppath
            })
            .then((response) =>
            {
                chrome.runtime.sendMessage(
                {
                    message: "waittimer"
                ,   Interval: 200
                })
                .then((response2) =>
                {
                    if (response2.Res === "ok")
                    {
                        callback(response);
                    }
                })
                ;
            })
            ;
        }
    }
}

function setbody(psubclass, ptarget)
{
    let elem = document.querySelector(".XXBoxDriveBody");
    if (elem === null)
    {
        let tmpelm = document.createElement("div");
        tmpelm.className = "XXBoxDriveBody";
        tmpelm.style.display = "none";
        tmpelm.setAttribute("xxbdclass", psubclass);
        document.querySelector(".XXBoxDriveDummy").appendChild(tmpelm);
        ptarget.addEventListener("mousemove", (event) =>
        {
            //  mousemove時処理
            let elem = document.querySelector(".XXBoxDriveBody");
            let psubclass = elem.getAttribute("xxbdclass");
            Array.from(document.querySelectorAll("." + psubclass)).map(elem =>
            {
                elem.style.display = "flex";
            })
            ;
        });
        ptarget.addEventListener("mouseover", (event) =>
        {
            //  mouseover時処理
            let elem = document.querySelector(".XXBoxDriveBody");
            let psubclass = elem.getAttribute("xxbdclass");
            Array.from(document.querySelectorAll("." + psubclass)).map(elem =>
            {
                elem.style.display = "flex";
            })
            ;
        });
        ptarget.addEventListener("mouseleave", (event) =>
        {
            //  mouseleave時処理
            let elem = document.querySelector(".XXBoxDriveBody");
            let psubclass = elem.getAttribute("xxbdclass");
            Array.from(document.querySelectorAll("." + psubclass)).map(elem =>
            {
                elem.style.display = "none";
            })
            ;
        });
    }
    if (elem.getAttribute("xxbdclass") !== psubclass)
    {
        elem.setAttribute("xxbdclass", psubclass);
    }
}

function reposDummyFolder()
{
    let tmpelm = document.querySelector(".action-bar-title").querySelector(".XXBoxDriveDummy");
    let tmpbaserect = document.querySelector(".page-content.files-page").getBoundingClientRect();
    tmpelm.style.left = (
        document.querySelector(".action-bar-title").querySelector(".ItemListBreadcrumb-listItem.is-last").getBoundingClientRect().left
        - tmpbaserect.left
        ).toString() + "px";
    let tmprect = document.querySelector(".page-title").getBoundingClientRect();
    tmpelm.style.top = (
        tmprect.top
        + tmprect.height
        - (tmprect.top - document.querySelector(".page-title").parentElement.getBoundingClientRect().top)
        - tmpbaserect.top
        ).toString() + "px";
}

function boxfolder(pid, ppath, pautoopen)
{
    try
    {
        let wurl = String(window.location);
        if (wurl.match(/https:[/][/].*[.]box[.]com[/](folder)[/][0-9a-zA-Z]*.*/) == null)   //  box folder
        {
            dbgPutlogBoxDrive(3, "boxfolder0a", "skip");
            return "skip";
        }
        dbgPutlogBoxDrive(2, "boxfolder00", window.location + "," + pid + "," + ppath);

        let wid = wurl.replace(/http.*[/]folder[/]([0-9a-zA-Z]*).*/, "$1");     //  window.location.replaceというメソッドあり。注意
        if (pid !== wid)
        {
            dbgPutlogBoxDrive(3, "boxfolder0b", "diff");
            return "diff";
        }

        let parentelm = null;
        if (pautoopen === false)
        {
            if (mode_boxlink === null)
            {
                dbgPutlogBoxDrive(3, "boxfolder", "checkOption");
                chrome.runtime.sendMessage(
                {
                    message: "checkOption"
                })
                .then((response) =>
                {
                    if (response.Res.startsWith("ok,") === true)
                    {
                        if (response.Res.indexOf(",makelink,") !== -1)
                        {
                            varelem.setAttribute("xxbdboxlink", "true");
                            mode_boxlink = true;
                        }
                        else
                        {
                            varelem.setAttribute("xxbdboxlink", "false");
                            mode_boxlink = false;
                        }
                        if (response.Res.indexOf(",closefoldersidebar,") !== -1)
                        {
                            varelem.setAttribute("xxbdslidebarfolder", "false");
                            mode_slidebar_folder = false;   //  隠す
                        }
                        else
                        {
                            varelem.setAttribute("xxbdslidebarfolder", "true");
                            mode_slidebar_folder = true;    //  隠さない
                        }
                        dbgPutlogBoxDrive(3, "boxfolder", "checkOption," + mode_boxlink + "," + mode_slidebar_folder);
                        chrome.runtime.sendMessage(
                        {
                            message: "waittimer"
                        ,   Interval: 10
                        })
                        .then((response2) =>
                        {
                            if (response2.Res === "ok")
                            {
                                return boxfolder(pid, ppath, pautoopen);    //  再帰
                            }
                        })
                        ;
                    }
                });
                dbgPutlogBoxDrive(3, "boxfolder0d", "skip");
                return "skip";
            }

            parentelm = document.querySelector(".action-bar-title");
            if (parentelm === null)
            {
                dbgPutlogBoxDrive(3, "boxfolder0c", "skip");
                return "skip";  //  追加先のエレメントが未だ存在しない→skip
            }

            let tmpelm = parentelm.querySelector(".XXBoxDriveDummy");
            if (tmpelm === null)
            {
                tmpelm = document.createElement("div");
                tmpelm.className = "XXBoxDriveDummy ItemLabels-container";
                tmpelm.style.position = "absolute";
                tmpelm.innerHTML = "&nbsp;";
                tmpelm.setAttribute("xxbdpid", "xx"); //  後でセット
                parentelm.appendChild(tmpelm);
                reposDummyFolder();
                window.addEventListener("resize", (event) =>
                {
                    reposDummyFolder();
                });
            }
            var elmxxbd = tmpelm;
            if (mode_opener !== true)
            {
                let tmpelm = document.querySelector('.Body').querySelector(".XXBoxDriveOption");
                if (tmpelm === null)
                {
                    tmpelm = document.createElement("div");
                    tmpelm.className = "XXBoxDriveOption XXBoxDriveCopy";
                    tmpelm.style.position = "absolute";
                    tmpelm.style.left = "0px";
                    tmpelm.style.top = "0px";
                    tmpelm.innerHTML = "&nbsp;";
                    tmpelm.style.zIndex = 2147483647;
                    tmpelm.style.display = "none";
                    document.querySelector('.Body').appendChild(tmpelm);
                    tmpelm = addupdbutton(document.querySelector('.Body').querySelector(".XXBoxDriveOption")
                        , "XXBoxDriveOptionOpen", ""
                        , BTNOPTION_TEXT, BTNOPTION_HINT
                        , "openOption", "", "", ""
                        );
                    tmpelm.disabled = false;
                }
                setbody("XXBoxDriveCopy", document.querySelector('.Body'));
            }
            else
            {
                let tmpelm = document.querySelector('.Body').querySelector(".XXBoxDriveOption");
                if (tmpelm !== null)
                {
                    tmpelm.remove();
                }
                setbody("XXBDViewFolder", document.querySelector('.Body'));
            }
        }

        dbgPutlogBoxDrive(2, "ppath", ppath);
        let pathLast = "";
        pathLast += [...document.querySelectorAll(".ItemListBreadcrumb-listItem")].map((v) => v.innerText).filter((v) => v !== "" && v !== "すべてのファイル").join("/");
        dbgPutlogBoxDrive(2, "pathLast", pathLast);
        if ((mode_opener !== true) || (ppath !== "") && (ppath.startsWith("%") !== true) && (ppath.endsWith("/" + pathLast) === true))
        {
            //  getfolderで取得でき、末尾が表示しているページのフォルダ内容と一致
            if (pautoopen === false)
            {
                let tmpelm = null;
                if (mode_opener !== true)
                {
                    tmpelm = addupdbutton(elmxxbd
                        , "XXBoxDriveCopy", "XXBDViewFolder"  //" breadcrumbs"
                        , BTNCC_TEXT, BTNCC_HINT
                        , "copyBoxDrive", "", String(window.location), pathLast
                        );
                }
                else
                {
                    tmpelm = addupdbutton(elmxxbd
                        , "XXBoxDriveCopy", "XXBDViewFolder"  //" breadcrumbs"
                        , BTNCC_TEXT, BTNCC_HINT
                        , "copyBoxDrive", "", String(window.location), ""
                        );
                }
                tmpelm.disabled = false;
                tmpelm = addupdbutton(elmxxbd
                    , "XXBoxDriveOpen", "XXBDViewFolder"  //" breadcrumbs"
                    , BTNOPEN_TEXT, BTNOPEN_HINT
                    , "openBoxDrive", "", String(window.location), ""
                    );
                tmpelm.disabled = false;

                if (mode_boxlink === true)
                {
                    //  ショートカットの作成
                    //  カレントフォルダ
                    if (ppath === "/")
                    {
                        tmpelm = parentelm.querySelector(".XXBoxDriveMakeLink");
                        if (tmpelm !== null)
                        {
                            tmpelm.style.display = "none";
                        }
                    }
                    else
                    {
                        tmpelm = parentelm.querySelector(".XXBoxDriveMakeLink");
                        tmpelm = addupdbutton(elmxxbd
                            , "XXBoxDriveMakeLink", "XXBDViewFolder"
                            , BTNMKSC_TEXT, BTNMKSC_HINT
                            , "makelinkBoxDrive", pid, wurl, ppath
                            );
                        tmpelm.disabled = false;
                    }
                }

                Array.from(document.querySelectorAll(".TableRow-focusBorder")).map(elem =>
                {
                    let parentelm = elem.querySelector(".item-name-holder");
                    if (parentelm === null)
                    {
                        dbgPutlogBoxDrive(3, "boxfolder0e", "skip");
                        return "skip";  //  追加先のエレメントが未だ存在しない→skip
                    }
                    let tmpelm = elem.querySelector(".file-list-date");
                    if (tmpelm === null)
                    {
                        dbgPutlogBoxDrive(3, "boxfolder0f", "skip");
                        return "skip";  //  追加先のエレメントが未だ準備中→skip
                    }
                    tmpelm = parentelm.querySelector(".ItemLabels-container");
                    if (tmpelm === null)
                    {
                        tmpelm = document.createElement("div");
                        tmpelm.className = "ItemLabels-container";
                        parentelm.appendChild(tmpelm);
                    }
                    tmpelm = parentelm.querySelector(".XXBoxDriveCopy");
                    if (tmpelm === null)
                    {
                        let wurl = "";
                        let wid = "";
                        let wname = "";
                        let wboxlnk = false;
                        let wtags = elem.querySelectorAll("a[href]");
                        for(let i in wtags)
                        {
                            if (wtags.hasOwnProperty(i))
                            {
                                if (((wtags[i]).href).indexOf("/folder/") !== -1)
                                {
                                    wurl = (wtags[i]).href;
                                    wid = wurl.replace(/http.*[/]folder[/]([0-9a-zA-Z]*).*/, "$1");     //  window.location.replaceというメソッドあり。注意
                                    wname = (wtags[i]).textContent;
                                }
                                else
                                if (((wtags[i]).href).indexOf("/file/") !== -1)
                                {
                                    if ((wtags[i]).textContent.toLowerCase().endsWith(".boxlnk") === true)
                                    {
                                        wboxlnk = true;
                                    }
                                    wurl = (wtags[i]).href;
                                    wid = wurl.replace(/http.*[/]file[/]([0-9a-zA-Z]*).*/, "$1");     //  window.location.replaceというメソッドあり。注意
                                    wname = (wtags[i]).textContent;
                                }
                            }
                        }

                        if (wid !== "")
                        {
                            dbgPutlogBoxDrive(3, "boxfolder", "addupdbutton," + "" + "," + wurl + "," + ppath + "/" + wname);
                            if (mode_opener !== true)
                            {
                                tmpelm = addupdbutton(parentelm.querySelector(".ItemLabels-container")
                                    , "XXBoxDriveCopy", "XXBDViewFolder"
                                    , BTNCC_TEXT, BTNCC_HINT
                                    , "copyBoxDrive", "", wurl, pathLast + "/" + wname
                                    );
                            }
                            else
                            {
                                tmpelm = addupdbutton(parentelm.querySelector(".ItemLabels-container")
                                    , "XXBoxDriveCopy", "XXBDViewFolder"
                                    , BTNCC_TEXT, BTNCC_HINT
                                    , "copyBoxDrive", "", wurl, ppath + "/" + wname
                                    );
                            }
                            tmpelm.disabled = false;
                            tmpelm = addupdbutton(parentelm.querySelector(".ItemLabels-container")
                                , "XXBoxDriveOpen", "XXBDViewFolder"
                                , BTNOPEN_TEXT, BTNOPEN_HINT
                                , "openBoxDrive", "", wurl, ppath + "/" + wname
                                );
                            tmpelm.disabled = false;
                            if ((mode_boxlink === true) && (wboxlnk === false))
                            {
                                dbgPutlogBoxDrive(3, "boxfolder", "addupdbutton2," + wid + "," + wurl + "," + ppath + "/" + wname);
                                tmpelm = addupdbutton(parentelm.querySelector(".ItemLabels-container")
                                    , "XXBoxDriveMakeLink", "XXBDViewFolder"
                                    , BTNMKSC_TEXT, BTNMKSC_HINT
                                    , "makelinkBoxDrive", wid, wurl, ppath + "/" + wname
                                    );
                                tmpelm.disabled = false;
                            }
                            if (wboxlnk === true)
                            {
                                dbgPutlogBoxDrive(4, "boxfolder", "addupdbutton3," + "" + "," + wurl + "," + "");
                                tmpelm = addupdbutton(parentelm.querySelector(".ItemLabels-container")
                                    , "XXBoxDriveOpenLink", "XXBDViewFolder"
                                    , BTNSCOPEN_TEXT, BTNSCOPEN_HINT
                                    , "openBoxDrive", "", wurl, ""
                                    );
                                tmpelm.disabled = false;
                                tmpelm = addupdbutton(parentelm.querySelector(".ItemLabels-container")
                                    , "XXBoxDriveOpenLinkBrowser", "XXBDViewFolder"
                                    , BTNSCLINKOPEN_TEXT, BTNSCLINKOPEN_HINT
                                    , "openBoxBrowser", "", wurl, ""
                                    );
                                tmpelm.disabled = false;
                            }
                        }
                        if (elem.querySelector(".file-list-date").querySelector(".ItemLabels-container") === null)
                        {
                            dbgPutlogBoxDrive(3, "boxfolder", "getTimestamp");
                            chrome.runtime.sendMessage(
                            {
                                message: "getTimestamp"
                            ,   Path: ppath + "/" + wname
                            })
                            .then((response) =>
                            {
                                if (response.Res.startsWith("ok,") === true)
                                {
                                    dbgPutlogBoxDrive(3, "boxfolder", "getTimestamp," + response.Res);
                                    let tmpelm2 = document.createElement("div");
                                    tmpelm2.className = "ItemLabels-container";
                                    elem.querySelector(".file-list-date").appendChild(tmpelm2);

                                    const BG_NA_COLOR = "#fffff0";
                                    const FG_NA_COLOR = "silver";

                                    let tmpelm3 = document.createElement("span");
                                    tmpelm3.className = "ItemLabels-container";
                                    tmpelm3.textContent = response.Res.slice("ok,".length + 10);    //  時刻のみ
                                    tmpelm3.disabled = true;
                                    tmpelm3.style.backgroundColor = BG_NA_COLOR;
                                    tmpelm3.style.color = FG_NA_COLOR;
                                    tmpelm2.appendChild(tmpelm3);
                                }
                                return;
                            });
                        }
                    }
                });
                if (elmxxbd.getAttribute("xxbdpid") !== pid)
                {
                    chrome.runtime.sendMessage(
                    {
                        message: "waittimer"
                    ,   Interval: 500
                    })
                    .then((response2) =>
                    {
                        dbgPutlogBoxDrive(3, "boxfolder", "fireMouseMove");
                        chrome.runtime.sendMessage(
                        {
                            message: "fireMouseMove"
                        })
                        ;
                        {
                            if (mode_slidebar_folder === true)
                            {
                                //  自動で開く
                                tmpelm = document.querySelector('.SidebarToggleButton');    //  [data-resin-target="showsidebar"]
                                if (tmpelm !== null)
                                {
                                    //  ボタンの位置が右端なら
                                    if (tmpelm.getBoundingClientRect().right >= (window.innerWidth * 0.8))
                                    {
                                        dbgPutlogBoxDrive(3, "boxfolder", "SidebarToggleButton表示");
                                        tmpelm.click();     //  スライドバーを表示
                                    }
                                }
                            }
                            else
                            if (mode_slidebar_folder === false)
                            {
                                //  自動で閉じる
                                tmpelm = document.querySelector('.SidebarToggleButton');    //  [data-resin-target="hidesidebar"]
                                if (tmpelm !== null)
                                {
                                    if (tmpelm.getBoundingClientRect().right < (window.innerWidth * 0.8))
                                    {
                                        //  ボタンの位置が右端でなければ
                                        //  （data-resin-targetが間違っていることがある）
                                        dbgPutlogBoxDrive(3, "boxfolder", "SidebarToggleButton隠す");
                                        tmpelm.click();     //  スライドバーを隠す
                                    }
                                }
                            }
                        }
                        elmxxbd.setAttribute("xxbdpid", pid);
                    })
                    ;
                }
            }
            return "same";
        }

        let path = "";
        if ([...document.querySelectorAll(".ItemListBreadcrumb-listItem")].map((v) => v.innerText).filter((v) => v !== "" && v == "すべてのファイル") == "すべてのファイル")
        {
            //  すべてのファイルで始まっている
            dbgPutlogBoxDrive(3, "boxfolder", "folder/0");
        }
        else
        {
            //  フォルダ階層取得
            const dotButton = document.querySelectorAll(".ItemListBreadcrumb > button")[0];
            if (dotButton)
            {
                let dotButton2 = document.querySelectorAll("a[data-resin-target='openfolder'].menu-item")[0];
                if (dotButton2)
                {
                    path += [ ...document.querySelectorAll("a[data-resin-target='openfolder'].menu-item"), ].map((e) => e.innerText).filter((v) => v !== "すべてのファイル").join("/");
                    if (!path.endsWith("/")) path += "/";
                    dotButton.click();
                }
                else
                {
                    dbgPutlogBoxDrive(3, "boxfolder", "openfolder");
                    dotButton.click();  //  開く
                    chrome.runtime.sendMessage(
                    {
                        message: "waittimer"
                    ,   Interval: 300
                    })
                    .then((response2) =>
                    {
                        if (response2.Res === "ok")
                        {
                            boxfolder(pid, ppath, pautoopen);   //  再帰
                        }
                    })
                    ;
                    return "openfolder";
                }
            }
        }
        dbgPutlogBoxDrive(2, "boxfolder", "path=" + path);
        path += pathLast;

        if (mode_opener === true)
        {
            //if (path != "")
            {
                if (path.startsWith("/") === false) path = "/" + path;
                //path = "%USERPROFILE%/Box" + path;
            }
            if (path !== "/")
            {
                dbgPutlogBoxDrive(3, "boxfolder", "saveBoxDrive," + wid + "," + path);
                chrome.runtime.sendMessage(
                {
                    message: "saveBoxDrive"
                ,   Id: wid
                ,   Path: path
                });
            }
            return "save";
        }
        else
        {
        }
    }
    finally
    {
        dbgPutlogBoxDrive(2, "boxfolder99", window.location);
    }
}

function reposDummyFile()
{
    let tmpelm = document.querySelector(".breadcrumb-item-last").querySelector(".XXBoxDriveDummy");
    let tmpbaserect = document.querySelector(".preview-header").getBoundingClientRect();
    let tmprect = document.querySelector(".preview-header").querySelector(".parent-name").getBoundingClientRect();
    tmpelm.style.left = (
        tmprect.left
        - tmpbaserect.left
        //+ (document.querySelector(".preview-header-title-section").getBoundingClientRect().left - tmpbaserect.left)
        ).toString() + "px";
    tmpelm.style.top = (
        tmprect.top
        + tmprect.height * 0.9
        //- (tmprect.top - document.querySelector(".preview-header-title-section").getBoundingClientRect().top)
        - tmpbaserect.top
        ).toString() + "px";
}

var count_retry = 0;
function boxfile(pid, ppath, pautoopen)
{
    try
    {
        var wurl = String(window.location);
        if (wurl.match(/https:[/][/].*[.]box[.]com[/](file)[/][0-9a-zA-Z]*.*/) == null)   //  box file
        {
            dbgPutlogBoxDrive(3, "boxfile0a", "skip");
            return "skip";
        }
        dbgPutlogBoxDrive(2, "boxfile00", window.location + "," + pid + "," + ppath);

        var wid = wurl.replace(/http.*[/]file[/]([0-9a-zA-Z]*).*/, "$1");     //  window.location.replaceというメソッドあり。注意
        if (pid !== wid)
        {
            dbgPutlogBoxDrive(3, "boxfile0c", "diff");
            return "diff";
        }

        var parentelm = null;
        if (pautoopen === false)
        {
            if (mode_boxlink === null)
            {
                dbgPutlogBoxDrive(3, "boxfile", "checkOption");
                chrome.runtime.sendMessage(
                {
                    message: "checkOption"
                })
                .then((response) =>
                {
                    if (response.Res.startsWith("ok,") === true)
                    {
                        if (response.Res.indexOf(",makelink,") !== -1)
                        {
                            varelem.setAttribute("xxbdboxlink", "true");
                            mode_boxlink = true;
                        }
                        else
                        {
                            varelem.setAttribute("xxbdboxlink", "false");
                            mode_boxlink = false;
                        }
                        if (response.Res.indexOf(",closefilesidebar,") !== -1)
                        {
                            varelem.setAttribute("xxbdslidebarfile", "false");
                            mode_slidebar_file = false;   //  隠す
                        }
                        else
                        {
                            varelem.setAttribute("xxbdslidebarfile", "true");
                            mode_slidebar_file = true;    //  隠さない
                        }
                        dbgPutlogBoxDrive(3, "boxfile", "checkOption," + mode_boxlink + "," + mode_slidebar_file);
                        chrome.runtime.sendMessage(
                        {
                            message: "waittimer"
                        ,   Interval: 10
                        })
                        .then((response2) =>
                        {
                            if (response2.Res === "ok")
                            {
                                return boxfile(pid, ppath, pautoopen);    //  再帰
                            }
                        })
                        ;
                    }
                });
                dbgPutlogBoxDrive(3, "boxfile0e", "skip");
                return "skip";
            }

            parentelm = document.querySelector(".breadcrumb-item-last");    //  .item-name  .preview-header-title-section
            if (parentelm === null)
            {
                dbgPutlogBoxDrive(3, "boxfile0b", "skip");
                return "skip";  //  追加先のエレメントが未だ存在しない→skip
            }

            let tmpelm = parentelm.querySelector(".XXBoxDriveDummy");
            if (tmpelm === null)
            {
                tmpelm = document.createElement("div");
                tmpelm.className = "XXBoxDriveDummy ItemLabels-container XXBDViewFile";
                tmpelm.style.position = "absolute";
                //tmpelm.style.display = "none";
                tmpelm.style.border = "solid 2px blue";
                tmpelm.style.backgroundColor = BG_NA_COLOR;
                tmpelm.style.zIndex = 2147483647;
                tmpelm.setAttribute("xxbdpid", "xx"); //  後でセット
                parentelm.insertBefore(tmpelm, parentelm.querySelector(".parent-section"));
                window.addEventListener("resize", (event) =>
                {
                    reposDummyFile();
                });
                dbgPutlogBoxDrive(3, "boxfile", "XXBoxDriveDummy作成");
            }
            setbody("XXBDViewFile", document.querySelector(".Body"));
            //setbody("XXBDViewFile", document.querySelector(".preview-header-left"));
            var elmxxbd = parentelm.querySelector(".XXBoxDriveDummy");
            if (mode_opener !== true)
            {
                let tmpelm = document.querySelector('.Body').querySelector(".XXBoxDriveOption");
                if (tmpelm !== null)
                {
                    tmpelm.remove();
                }
                return "same";
            }
        }

        var wurl = String(document.querySelector("a.parent-name").href);
        if (wurl.match(/https:[/][/].*[.]box[.]com[/]folder[/][0-9a-zA-Z]*.*/) === null)   //  box folder
        {
            dbgPutlogBoxDrive(3, "boxfile0d", "diff");
            return "diff";
        }
        var wfolderid = wurl.replace(/http.*[/]folder[/]([0-9a-zA-Z]*).*/, "$1");
        var file = document.querySelector(".preview-header-title-section > .item-name").textContent;
        dbgPutlogBoxDrive(3, "boxfile", "folderid+file=" + "%" + wfolderid + "%/" + file);
        if (count_retry < 5)    //  約10秒間
        {
            if (ppath === "(none)")
            {
                count_retry++;
            }
            else
            if (ppath === "(lost)")
            {
                //  親フォルダが未取得かも?
                count_retry++;
            }
            else
            if (ppath !== "%" + wfolderid + "%/" + file)
            {
                //  ファイル名が変わったかも?
                dbgPutlogBoxDrive(3, "boxfile0e", "diff");
                count_retry++;
                return "diff";
            }
            else
            {
                //  一致
                if (count_retry > 0)
                {
                    dbgPutlogBoxDrive(2, "boxfile0f", "diff解消," + count_retry);
                    count_retry = 0;
                }
            }
        }
        else
        {
            //  ファイル名が変わった!?
            //window.location.reload();   //  ★
            //return "reload";
            ppath = ">lost<";
        }
        let wpath = "%" + wfolderid + "%/" + file;
        if ((ppath === ">lost<") || (ppath === wpath))
        {
            //  getfolderで取得でき、内容一致
            //  または、未だフォルダid未取得
            if (pautoopen === false)
            {
                let tmpelm = elmxxbd.querySelector(".XXBoxDriveDummy1");
                if (tmpelm === null)
                {
                    tmpelm = document.createElement("span");
                    tmpelm.className = "XXBoxDriveDummy1";
                    tmpelm.textContent = "file:";
                    elmxxbd.appendChild(tmpelm);
                    tmpelm = document.createElement("span");
                    tmpelm.className = "XXBoxDriveDummy2";
                    tmpelm.textContent = " folder:";
                    elmxxbd.appendChild(tmpelm);
                }

                dbgPutlogBoxDrive(4, "boxfile", "addupdbutton," + "" + "," + String(window.location) + "," + wpath);
                tmpelm = addupdbutton(elmxxbd.querySelector(".XXBoxDriveDummy1")
                    , "XXBoxDriveCopy", ""  //" breadcrumbs"    //  breadcrumbs=省略なし
                    , BTNCC_TEXT, BTNCC_HINT
                    , "copyBoxDrive", "", String(window.location), wpath
                    );
                tmpelm.disabled = false;
                tmpelm = addupdbutton(elmxxbd.querySelector(".XXBoxDriveDummy1")
                    , "XXBoxDriveOpen", ""  //" breadcrumbs"
                    , BTNOPEN_TEXT, BTNOPEN_HINT
                    , "openBoxDrive", "", String(window.location), wpath
                    );
                tmpelm.disabled = false;
                if (wpath.endsWith(".boxlnk") === true)
                {
                    //  リンク先を開くの作成
                    dbgPutlogBoxDrive(4, "boxfile", "addupdbutton2," + "" + "," + String(window.location) + "," + "");
                    tmpelm = addupdbutton(elmxxbd.querySelector(".XXBoxDriveDummy1")
                        , "XXBoxDriveOpenLink", ""
                        , BTNSCOPEN_TEXT, BTNSCOPEN_HINT
                        , "openBoxDrive", "", String(window.location), ""
                        );
                    tmpelm.disabled = false;
                    tmpelm = addupdbutton(elmxxbd.querySelector(".XXBoxDriveDummy1")
                        , "XXBoxDriveOpenLinkBrowser", ""
                        , BTNSCLINKOPEN_TEXT, BTNSCLINKOPEN_HINT
                        , "openBoxBrowser", "", String(window.location), ""
                        );
                    tmpelm.disabled = false;
                    //  .boxlnkファイルの内容を表示する
                    let tmpelmp = document.querySelector(".bp-content");
                    if (tmpelmp !== null)
                    {
                        if ((tmpelmp.querySelector(".XXBoxDriveContent") === null)
                        || (tmpelmp.querySelector(".XXBoxDriveContent").getAttribute("xxbdpath") !== wpath))
                        {
                            dbgPutlogBoxDrive(3, "boxfile", "getData," + wpath);
                            tmpelm = document.createElement("div");
                            tmpelm.setAttribute("xxbdmessage", "getData");
                            tmpelm.setAttribute("xxbdid", "");
                            tmpelm.setAttribute("xxbdurl", "");
                            tmpelm.setAttribute("xxbdpath", wpath);
                            clickmain(tmpelm, function(response)
                            {
                                dbgPutlogBoxDrive(3, "boxfile", "getData," + response.Res);
                                if (response.Res.startsWith("ok,") === true)
                                {
                                    let tmpurl = response.Res.slice("ok,".length).slice(0, response.Res.indexOf("\n") - "ok,".length);
                                    let tmpetc = response.Res.slice("ok,".length + tmpurl.length);
                                    let tmpelmp2 = tmpelmp.querySelector(".XXBoxDriveContent");
                                    if (tmpelmp2 === null)
                                    {
                                        tmpelm = document.createElement("div");
                                        tmpelm.className = "XXBoxDriveContent";
                                        tmpelm.style.position = "absolute";
                                        tmpelm.style.left = "0px";
                                        tmpelm.style.top = (
                                            document.querySelector(".breadcrumb-item-last").getBoundingClientRect().height * 2
                                            ).toString() + "px";
                                        tmpelm.style.border = "solid 5px " + BG_NA_COLOR;
                                        tmpelm.style.backgroundColor = BG_NA_COLOR; //  "white";
                                        tmpelm.style.color = "black";
                                        tmpelmp.prepend(tmpelm);
                                        tmpelmp2 = tmpelm;

                                        tmpelm = document.createElement("div");
                                        tmpelm.className = "XXBoxDriveContentUrl";
                                        tmpelmp2.appendChild(tmpelm);
                                        tmpelm = document.createElement("div");
                                        tmpelm.className = "XXBoxDriveContentName";
                                        tmpelmp2.appendChild(tmpelm);
                                    }
                                    {
                                        tmpelm = addupdbutton(tmpelmp2.querySelector(".XXBoxDriveContentUrl")
                                            , "XXBoxDriveOpenLinkBrowser", ""
                                            , tmpurl, BTNSCOPEN_HINT
                                            , "openBoxBrowser", "", String(window.location), ""
                                            );
                                        tmpelm.disabled = false;
                                        tmpelm.style.color = "blue";
                                        tmpelm.style.textAlign = "left";
                                        tmpelm.style.fontSize = "100%";
                                        tmpelm.addEventListener("mouseleave", (event) =>
                                        {
                                            //  mouseleave時処理
                                            event.target.style.color = "blue";
                                            event.target.style.backgroundColor = BG_NA_COLOR;
                                        });
                                        tmpelm = addupdbutton(tmpelmp2.querySelector(".XXBoxDriveContentName")
                                            , "XXBoxDriveOpenLink", ""
                                            , tmpetc.replaceAll("\r\n", "").replaceAll("\n", ""), BTNSCLINKOPEN_HINT
                                            , "openBoxDrive", "", String(window.location), ""
                                            );
                                        tmpelm.disabled = false;
                                        tmpelm.style.color = "black";
                                        tmpelm.style.textAlign = "left";
                                        tmpelm.style.fontSize = "100%";
                                        tmpelm.addEventListener("mouseleave", (event) =>
                                        {
                                            //  mouseleave時処理
                                            event.target.style.color = "black";
                                            event.target.style.backgroundColor = BG_NA_COLOR;
                                        });
                                    }
                                    tmpelmp2.setAttribute("xxbdpath", wpath);
                                    //  エラー表示を隠す
                                    tmpelm = document.querySelector(".bp-error");
                                    if (tmpelm !== null)
                                    {
                                        if (tmpelm.style.display !== "none")
                                        {
                                            dbgPutlogBoxDrive(2, "boxfile", "エラー表示を隠す2");
                                            tmpelm.style.display = "none";
                                        }
                                    }
                                }
                            });
                        }
                    }
                }
                else
                {
                    {
                        tmpelm = document.querySelector(".bp-content").querySelector(".XXBoxDriveContent");
                        if (tmpelm !== null)
                        {
                            tmpelm.remove();
                        }
                        tmpelm = elmxxbd.querySelector(".XXBoxDriveDummy1").querySelector(".XXBoxDriveOpenLinkBrowser");
                        if (tmpelm !== null)
                        {
                            tmpelm.remove();
                        }
                        tmpelm = elmxxbd.querySelector(".XXBoxDriveDummy1").querySelector(".XXBoxDriveOpenLink");
                        if (tmpelm !== null)
                        {
                            tmpelm.remove();
                        }
                    }
                    if (mode_boxlink === true)
                    {
                        //  ショートカットの作成
                        dbgPutlogBoxDrive(4, "boxfile", "addupdbutton3," + pid + "," + String(window.location) + "," + wpath);
                        tmpelm = addupdbutton(elmxxbd.querySelector(".XXBoxDriveDummy1")
                            , "XXBoxDriveMakeLink", ""
                            , BTNMKSC_TEXT, BTNMKSC_HINT
                            , "makelinkBoxDrive", pid, String(window.location), wpath
                            );
                        tmpelm.disabled = false;
                    }
                }

                {
                    dbgPutlogBoxDrive(4, "boxfile", "addupdbutton4," + "" + "," + wurl + "," + "");
                    tmpelm = addupdbutton(elmxxbd.querySelector(".XXBoxDriveDummy2")
                        , "XXBoxDriveCopy", ""
                        , BTNCC_TEXT, BTNCC_HINT
                        , "copyBoxDrive", "", wurl, ""
                        );
                    tmpelm.disabled = false;
                    tmpelm = addupdbutton(elmxxbd.querySelector(".XXBoxDriveDummy2")
                        , "XXBoxDriveOpen", ""
                        , BTNOPEN_TEXT, BTNOPEN_HINT
                        , "openBoxDrive", "", wurl, ""
                        );
                    tmpelm.disabled = false;
                }
                if (elmxxbd.getAttribute("xxbdpid") !== pid)
                {
                    reposDummyFile();
                    chrome.runtime.sendMessage(
                    {
                        message: "waittimer"
                    ,   Interval: 200
                    })
                    .then((response2) =>
                    {
                        dbgPutlogBoxDrive(3, "boxfolder", "fireMouseMove");
                        chrome.runtime.sendMessage(
                        {
                            message: "fireMouseMove"
                        })
                        ;
                        {
                            if (mode_slidebar_file === true)
                            {
                                //  自動で開く
                                tmpelm = document.querySelector('.bdl-SidebarToggleButton[aria-label="サイドバーを表示"]');
                                if (tmpelm !== null)
                                {
                                    dbgPutlogBoxDrive(3, "boxfile", "bdl-SidebarToggleButton表示");
                                    tmpelm.click();     //  スライドバーを表示
                                }
                            }
                            else
                            if (mode_slidebar_file === false)
                            {
                                //  自動で閉じる
                                tmpelm = document.querySelector('.bdl-SidebarToggleButton[aria-label="サイドバーを非表示"]');
                                if (tmpelm !== null)
                                {
                                    dbgPutlogBoxDrive(3, "boxfile", "bdl-SidebarToggleButton隠す");
                                    tmpelm.click();     //  スライドバーを隠す
                                }
                            }
                        }
                        elmxxbd.setAttribute("xxbdpid", pid);
                    })
                    ;
                }
            }
            return "same";
        }

        if (pautoopen === false)
        {
            dbgPutlogBoxDrive(3, "boxfile", "saveBoxDrive," + wid + "," + wpath);
            chrome.runtime.sendMessage(
            {
                message: "saveBoxDrive"
            ,   Id: wid
            ,   Path: wpath
            })
            .then((response) => 
            {
            })
            ;
            return "save";
        }
        else
        {
            //  親フォルダを取得
            dbgPutlogBoxDrive(3, "boxfile", "getBoxDrive," + wfolderid + "," + wurl);
            chrome.runtime.sendMessage(
            {
                message: "getBoxDrive"
            ,   Id: wfolderid
            ,   Url: wurl
            })
            .then((response) => 
            {
                if (response.Path === "(none)")
                {
                    return "nofolder";
                }
                else
                {
                    dbgPutlogBoxDrive(3, "boxfile", "saveBoxDrive," + wid + "," + wpath);
                    chrome.runtime.sendMessage(
                    {
                        message: "saveBoxDrive"
                    ,   Id: wid
                    ,   Path: wpath
                    })
                    .then((response) => 
                    {
                        return "save";
                    })
                    ;
                }
            });
        }
    }
    finally
    {
        dbgPutlogBoxDrive(2, "boxfile99", window.location);
    }
}

function boxcollection()
{
//      try
//      {
//          dbgPutlogBoxDrive(2, "boxcollection00", window.location);

//          Array.from(document.querySelectorAll(".TableRow-focusBorder")).map(elem =>
//          {
//              let parentelm = elem.querySelector(".item-name-holder");
//              if (parentelm === null)
//              {
//                  dbgPutlogBoxDrive(3, "boxcollection0a", "skip");
//                  return "skip";  //  追加先のエレメントが未だ存在しない→skip
//              }
//              let tmpelm = elem.querySelector(".file-list-date");
//              if (tmpelm === null)
//              {
//                  dbgPutlogBoxDrive(3, "boxcollection0b", "skip");
//                  return "skip";  //  追加先のエレメントが未だ準備中→skip
//              }
//              tmpelm = parentelm.querySelector(".ItemLabels-container");
//              if (tmpelm === null)
//              {
//                  tmpelm = document.createElement("div");
//                  tmpelm.className = "ItemLabels-container";
//                  parentelm.insertBefore(tmpelm, parentelm.querySelector(".collection-item-folder-subsection"));
//              }
//              tmpelm = parentelm.querySelector(".XXBoxDriveCopy");
//              if (tmpelm === null)
//              {
//                  {
//                      let wurl = "";
//                      let wid = "";
//                      let wboxlnk = false;
//                      let wtags = elem.querySelector(".item-name").querySelectorAll("a[href]");
//                      for(let i in wtags)
//                      {
//                          if (wtags.hasOwnProperty(i))
//                          {
//                              if (((wtags[i]).href).indexOf("/folder/") !== -1)
//                              {
//                                  wurl = (wtags[i]).href;
//                                  wid = wurl.replace(/http.*[/]folder[/]([0-9a-zA-Z]*).*/, "$1");     //  window.location.replaceというメソッドあり。注意
//                              }
//                              else
//                              if (((wtags[i]).href).indexOf("/file/") !== -1)
//                              {
//                                  if ((wtags[i]).textContent.toLowerCase().endsWith(".boxlnk") === true)
//                                  {
//                                      wboxlnk = true;
//                                  }
//                                  wurl = (wtags[i]).href;
//                                  wid = wurl.replace(/http.*[/]file[/]([0-9a-zA-Z]*).*/, "$1");     //  window.location.replaceというメソッドあり。注意
//                              }
//                          }
//                      }

//                      if (wid !== "")
//                      {
//                          dbgPutlogBoxDrive(4, "boxcollection", "addupdbutton," + "" + "," + wurl + "," + "");
//                          tmpelm = addupdbutton(parentelm.querySelector(".ItemLabels-container")
//                              , "XXBoxDriveCopy", "XXBDView"
//                              , BTNCC_TEXT, BTNCC_HINT
//                              , "copyBoxDrive", "", wurl, ""
//                              );
//                          tmpelm.disabled = false;
//                          tmpelm = addupdbutton(parentelm.querySelector(".ItemLabels-container")
//                              , "XXBoxDriveOpen", "XXBDView"
//                              , BTNOPEN_TEXT, BTNOPEN_HINT
//                              , "openBoxDrive", "", wurl, ""
//                              );
//                          tmpelm.disabled = false;
//                          if (wboxlnk === true)
//                          {
//                              dbgPutlogBoxDrive(4, "boxcollection2", "addupdbutton," + "" + "," + wurl + "," + "");
//                              tmpelm = addupdbutton(parentelm.querySelector(".ItemLabels-container")
//                                  , "XXBoxDriveOpenLink", "XXBDView"
//                                  , BTNSCOPEN_TEXT, BTNSCOPEN_HINT
//                                  , "openBoxDrive", "", wurl, ""
//                                  );
//                              tmpelm.disabled = false;
//                              tmpelm = addupdbutton(parentelm.querySelector(".ItemLabels-container")
//                                  , "XXBoxDriveOpenLinkBrowser", "XXBDView"
//                                  , BTNSCLINKOPEN_TEXT, BTNSCLINKOPEN_HINT
//                                  , "openBoxBrowser", "", wurl, ""
//                                  );
//                              tmpelm.disabled = false;
//                          }
//                      }
//                  }

//                  {
//                      //  folder
//                      let wurl = "";
//                      let wid = "";
//                      let tmpelmp = elem.querySelector(".collection-item-folder-subsection");
//                      let wtags = tmpelmp.querySelectorAll("a[href]");
//                      for(let i in wtags)
//                      {
//                          if (wtags.hasOwnProperty(i))
//                          {
//                              if (((wtags[i]).href).indexOf("/folder/") !== -1)
//                              {
//                                  wurl = (wtags[i]).href;
//                                  wid = wurl.replace(/http.*[/]folder[/]([0-9a-zA-Z]*).*/, "$1");     //  window.location.replaceというメソッドあり。注意
//                              }
//                              else
//                              if (((wtags[i]).href).indexOf("/file/") !== -1)
//                              {
//                                  wurl = (wtags[i]).href;
//                                  wid = wurl.replace(/http.*[/]file[/]([0-9a-zA-Z]*).*/, "$1");     //  window.location.replaceというメソッドあり。注意
//                              }
//                          }
//                      }

//                      if (wid !== "")
//                      {
//                          dbgPutlogBoxDrive(4, "boxcollection", "addupdbutton3," + "" + "," + wurl + "," + "");
//                          tmpelm = addupdbutton(tmpelmp
//                              , "XXBoxDriveCopy", "XXBDView"
//                              , BTNCC_TEXT, BTNCC_HINT
//                              , "copyBoxDrive", "", wurl, ""
//                              );
//                          tmpelm.disabled = false;
//                          tmpelm = addupdbutton(tmpelmp
//                              , "XXBoxDriveOpen", "XXBDView"
//                              , BTNOPEN_TEXT, BTNOPEN_HINT
//                              , "openBoxDrive", "", wurl, ""
//                              );
//                          tmpelm.disabled = false;
//                      }
//                  }
//                  {
//                      if (mode_slidebar_folder === true)
//                      {
//                          //  自動で開く
//                          tmpelm = document.querySelector('.SidebarToggleButton');
//                          if (tmpelm !== null)
//                          {
//                              //  ボタンの位置が右端なら
//                              if (tmpelm.getBoundingClientRect().right >= (window.innerWidth * 0.8))
//                              {
//                                  dbgPutlogBoxDrive(3, "boxfolder", "SidebarToggleButton表示");
//                                  tmpelm.click();     //  スライドバーを表示
//                              }
//                          }
//                      }
//                      else
//                      if (mode_slidebar_folder === false)
//                      {
//                          //  自動で閉じる
//                          tmpelm = document.querySelector('.SidebarToggleButton');
//                          if (tmpelm !== null)
//                          {
//                              if (tmpelm.getBoundingClientRect().right < (window.innerWidth * 0.8))
//                              {
//                                  //  ボタンの位置が右端でなければ
//                                  //  （data-resin-targetが間違っていることがある）
//                                  dbgPutlogBoxDrive(3, "boxfolder", "SidebarToggleButton隠す");
//                                  tmpelm.click();     //  スライドバーを隠す
//                              }
//                          }
//                      }
//                  }
//              }
//          });
//          setbody();
//          return "do";

//      }
//      finally
//      {
//          dbgPutlogBoxDrive(2, "boxcollection99", window.location);
//      }
}

{
    //
}


//

chrome.runtime.onMessage.addListener(function(request, sender, sendResponse)
{
    if (request.method === "onUpdated, loading")
    {
        dbgPutlogBoxDrive(3, request.method, "000");
        dbgPutlog(3, request.method, "900");
        sendResponse({});
    }
    else
    if (request.method === "onUpdated, complete")
    {
        dbgPutlogBoxDrive(3, request.method, "000");
        dbgPutlog(3, request.method, "900");
        sendResponse({});
    }
    else
    if (request.method === "ShowOption")
    {
        dbgPutlogBoxDrive(3, request.method, "000");
        varelem = document.querySelector(".XXBoxDriveVar");
        if (varelem === null)
        {
            varelem = document.createElement("div");
            varelem.className = "XXBoxDriveVar";
            varelem.style.display = "none";
            varelem.setAttribute("xxbdopener", "false");
            varelem.setAttribute("xxbdboxlink", "");
            varelem.setAttribute("xxbdslidebarfolder", "false"); //  初期状態で、true=開く、false=閉じる
            varelem.setAttribute("xxbdslidebarfile", "true");    //  初期状態で、true=開く、false=閉じる
        }
//自動リロードはやめておく
//          if (varelem.getAttribute("xxbdopener") === "true")
//          {
//              window.location.reload();   //  ★
//              return; //  exit
//          }
        //  変数復元
        mode_opener = false;
        mode_boxlink = false;
        mode_slidebar_folder = str2bool(varelem.getAttribute("xxbdslidebarfolder"));
        mode_slidebar_file = str2bool(varelem.getAttribute("xxbdslidebarfile"));
        dbg = 1;
        dbgPutlogBoxDrive(3, request.method, "900");
        sendResponse({});
    }
    else
    if (request.method === "HideOption")
    {
        dbgPutlogBoxDrive(3, request.method, "000");
        varelem = document.querySelector(".XXBoxDriveVar");
        if (varelem === null)
        {
            varelem = document.createElement("div");
            varelem.className = "XXBoxDriveVar";
            varelem.style.display = "none";
            varelem.setAttribute("xxbdopener", "true");
            varelem.setAttribute("xxbdboxlink", "");
            varelem.setAttribute("xxbdslidebarfolder", "false"); //  初期状態で、true=開く、false=閉じる
            varelem.setAttribute("xxbdslidebarfile", "true");    //  初期状態で、true=開く、false=閉じる
        }
//自動リロードはやめておく
//          if (varelem.getAttribute("xxbdopener") === "true")
//          {
//              window.location.reload();   //  ★
//              return; //  exit
//          }
        //  変数復元
        mode_opener = true;
        mode_boxlink = str2bool(varelem.getAttribute("xxbdboxlink"));
        mode_slidebar_folder = str2bool(varelem.getAttribute("xxbdslidebarfolder"));
        mode_slidebar_file = str2bool(varelem.getAttribute("xxbdslidebarfile"));
        dbgPutlogBoxDrive(3, request.method, "900");
        sendResponse({});
    }
    else
    if (request.method === "getBoxDrive")
    {
        dbgPutlogBoxDrive(3, request.method, "000");
        dbgPutlogBoxDrive(3, "checkDebugLevel", "");
        chrome.runtime.sendMessage(
        {
            message: "checkDebugLevel"
        })
        .then((response) =>
        {
            if (dbg !== response.Level)
            {
                dbg = response.Level;
                dbgPutlogBoxDrive(1, "DebugLevel", dbg);
            }
        })
        ;
        var res = boxfolder(request.Id, request.Path, request.AutoOpen);
        dbgPutlogBoxDrive(3, request.method, "900, " + res);
        sendResponse({Res: res});
    }
    else
    if (request.method === "getBoxFile")
    {
        dbgPutlogBoxDrive(3, request.method, "000");
        dbgPutlogBoxDrive(3, "checkDebugLevel", "");
        chrome.runtime.sendMessage(
        {
            message: "checkDebugLevel"
        })
        .then((response) =>
        {
            dbg = response.Level;
            dbgPutlogBoxDrive(1, "DebugLevel", dbg);
        })
        ;
        var res = boxfile(request.Id, request.Path, request.AutoOpen);
        dbgPutlogBoxDrive(3, request.method, "900, " + res);
        sendResponse({Res: res});
    }
    else
    if (request.method === "getBoxCollection")
    {
        dbgPutlogBoxDrive(3, request.method, "000");
        dbgPutlogBoxDrive(3, "checkDebugLevel", "");
        chrome.runtime.sendMessage(
        {
            message: "checkDebugLevel"
        })
        .then((response) =>
        {
            dbg = response.Level;
            dbgPutlogBoxDrive(1, "DebugLevel", dbg);
        })
        ;
        var res = boxcollection();
        dbgPutlogBoxDrive(3, request.method, "900, " + res);
        sendResponse({Res: res});
    }
    else
    {
        sendResponse({});
    }
    return;
});
