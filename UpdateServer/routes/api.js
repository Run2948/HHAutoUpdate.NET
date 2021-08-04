const express = require('express')
const router = express.Router()
const fs = require('fs')
const path = require('path')

var uuid = require('uuid');

var defaultFolder = './uploads'

const multer = require('multer')

var createFolder = (folder) => {
  try {
    fs.accessSync(folder)
  } catch (error) {
    fs.mkdirSync(folder)
  }
};

createFolder(defaultFolder)

var storage = multer.diskStorage({
  destination(req, file, cb) {
    cb(null, defaultFolder)
  },
  filename(req, file, cb) {
    cb(null, uuid.v4() + path.extname(file.originalname))
  }
})
const uploadStorage = multer({ storage: storage })

var defaultServer = (req) => {
  return `${req.protocol
    }://${req.headers.host
    }`
}

Date.prototype.Format = function (fmt) {
  var o = {
    "M+": this.getMonth() + 1,
    "d+": this.getDate(),
    "h+": this.getHours(),
    "m+": this.getMinutes(),
    "s+": this.getSeconds(),
    "S": this.getMilliseconds()
  };
  if (/(y+)/.test(fmt)) {
    fmt = fmt.replace(RegExp.$1, (this.getFullYear() + "").substr(4 - RegExp.$1.length))
  }
  for (var k in o) {
    if (new RegExp("(" + k + ")").test(fmt)) {
      fmt = fmt.replace(RegExp.$1, (RegExp.$1.length == 1) ? (o[k]) : (("00" + o[k]).substr(("" + o[k]).length)));
    }
  }
  return fmt;
}

// query
router.get('/app', async (req, res) => {
  const query = req.query
  let app = query.app || "app"
  const server = defaultServer(req)
  const exists = await fs.existsSync(path.resolve(`./uploads/apps/${app}.json`))
  if (exists) {
    const info = require(`../uploads/apps/${app}.json`)
    info.ReleaseUrl = `${server}/api/download/${app}`
    res.send(info)
  } else {
    res.end()
  }
})

// update
router.post('/app', uploadStorage.single('file'), async (req, res) => {
  const app = req.body.appId;
  let info = {};
  info.ApplicationId = app;
  info.ApplicationStart = req.body.appName;
  info.ReleaseDate = new Date().Format("yyyyMMdd");
  info.ReleaseVersion = req.body.appVersion;
  info.ReleaseUrl = "./uploads/" + req.file.filename;
  info.UpdateMode = req.body.updateMode == "1" ? "Cover" : "NewInstall";
  info.VersionDesc = '\r\n' + req.body.versionDesc.trim();
  info.IgnoreFile = "";
  await fs.writeFileSync(path.resolve(`./uploads/apps/${app}.json`), JSON.stringify(info, null, "\t"));
  res.send({ code: 1, msg: 'Update succeeded!', data: info })
})

// download
router.get('/download/:name', async (req, res) => {
  const app = req.params.name;
  if (app == null || app == '') {
    res.send({ code: 404, msg: 'error' });
  }
  const exists = await fs.existsSync(path.resolve(`./uploads/apps/${app}.json`))
  if (exists) {
    const info = require(`../uploads/apps/${app}.json`)
    res.download(info.ReleaseUrl, `${info.ApplicationId
      }-V${info.ReleaseDate
      }.zip`)
  } else {
    res.end()
  }
})

// test
router.get('/test', (req, res) => {
  const server = defaultServer(req)
  res.send({
    "ApplicationId": "test-app",
    "ApplicationStart": "HHUpdate.Test",
    "ReleaseDate": "20210803",
    "ReleaseVersion": "1.0.0.1",
    "ReleaseUrl": `${server}/api/download/test-app`,
    "UpdateMode": "Cover",
    "VersionDesc": "\r\nAdd updater for your application at first time.",
    "IgnoreFile": ""
  })
})

module.exports = router
