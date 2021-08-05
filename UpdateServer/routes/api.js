const express = require('express')
const router = express.Router()
const path = require('path')
const {
  diskStorage,
  existsFile,
  writeJsonFile
} = require("../utils")

const uploadStorage = diskStorage('./uploads')

// query
router.get('/app', (req, res) => {
  const query = req.query
  let app = query.app || "app"
  const server = `${req.protocol}://${req.headers.host}`
  if (existsFile(path.resolve(`./uploads/apps/${app}.json`))) {
    const info = require(`../uploads/apps/${app}.json`)
    info.ReleaseUrl = `${server}/api/download/${app}`
    res.send(info)
  } else {
    res.end()
  }
})

// update
router.post('/app', uploadStorage.single('file'), (req, res) => {
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
  writeJsonFile(path.resolve(`./uploads/apps/${app}.json`), info);
  res.send({ code: 0, msg: 'Update succeeded!', data: info })
})

// download
router.get('/download/:name', (req, res) => {
  const app = req.params.name;
  if (app == null || app == '') {
    res.send({ code: 404, msg: 'error' });
  }
  if (existsFile(path.resolve(`./uploads/apps/${app}.json`))) {
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
  const server = `${req.protocol}://${req.headers.host}`
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
