const express = require('express')
const router = express.Router()
const path = require('path')
const {
  diskStorage,
  existsFile,
  writeJsonFile,
  backupJsonFile,
  readJsonFile
} = require("../utils")

const uploadStorage = diskStorage('./uploads')

// query
router.get('/app', (req, res) => {
  const { appId, appKey } = req.query
  const { auth, defaultKeys } = req.config
  if (auth) {
    if (!appKey || !defaultKeys.includes(appKey)) {
      res.end('Auth failed!')
    }
  }
  if (!appId || appId == null || appId == '') {
    res.end()
  }
  const server = `${req.protocol}://${req.headers.host}`
  if (existsFile(path.resolve(`./uploads/apps/${appId}.json`))) {
    const info = readJsonFile(path.resolve(`./uploads/apps/${appId}.json`))
    info.ReleaseUrl = `${server}/api/download?appId=${appId}?appKey=${appKey}`
    res.send(info)
  } else {
    res.end()
  }
})

// update
router.post('/app', uploadStorage.single('file'), (req, res) => {
  const { defaultUser } = req.config
  const { user, pwd } = req.session
  if (user && user == defaultUser.user
    && pwd && pwd == defaultUser.pwd
  ) {
    const appId = req.body.appId
    if (!appId || appId == null || appId == '') {
      res.end()
    }
    let info = {}
    info.ApplicationId = appId
    info.ApplicationStart = req.body.appName;
    info.ReleaseDate = new Date().Format("yyyyMMdd")
    info.ReleaseVersion = req.body.appVersion
    info.ReleaseUrl = "./uploads/" + req.file.filename
    info.UpdateMode = req.body.updateMode == "1" ? "Cover" : "NewInstall"
    info.VersionDesc = '\r\n' + req.body.versionDesc.trim()
    info.IgnoreFile = ""
    backupJsonFile(path.resolve(`./uploads/apps/${appId}.json`))
    writeJsonFile(path.resolve(`./uploads/apps/${appId}.json`), info)
    res.send({ code: 0, msg: 'Update succeeded!', data: info })
  } else {
    res.end('Authentication failed!')
  }
})

// download
router.get('/download', (req, res) => {
  const { appId, appKey } = req.query
  const { auth, defaultKeys } = req.config
  if (auth) {
    if (!appKey || !defaultKeys.includes(appKey)) {
      res.end('Auth failed!')
    }
  }
  if (!appId || appId == null || appId == '') {
    res.end()
  }
  if (existsFile(path.resolve(`./uploads/apps/${appId}.json`))) {
    const info = readJsonFile(path.resolve(`./uploads/apps/${appId}.json`))
    res.download(path.resolve(info.ReleaseUrl), `${info.ApplicationId}-V${info.ReleaseDate}.zip`)
  } else {
    res.end()
  }
})

// test
router.get('/test', (req, res) => {
  const { appKey } = req.query
  const { auth, defaultKeys } = req.config
  if (auth) {
    if (!appKey || !defaultKeys.includes(appKey)) {
      res.end('Auth failed!')
    }
  }
  const server = `${req.protocol}://${req.headers.host}`
  res.send({
    "ApplicationId": "test-app",
    "ApplicationStart": "HHUpdate.Test",
    "ReleaseDate": "20210803",
    "ReleaseVersion": "1.0.0.1",
    "ReleaseUrl": `${server}/api/download?appId=test-app` + auth ? `&appKey=${appKey}` : '',
    "UpdateMode": "Cover",
    "VersionDesc": "\r\nAdd updater for your application at first time.",
    "IgnoreFile": ""
  })
})

module.exports = router
