const express = require('express')
const router = express.Router()
const path = require('path')
const {
  initJsonFile
} = require("../utils")

router.get('/', (req, res) => {
  res.sendStatus(200)
})

router.get('/form', (req, res) => {
  initJsonFile(path.resolve(`./config.json`), { user: 'root', pwd: '1qaz@WSX' })
  const { user: defaultUser, pwd: defaultPwd } = require(`../config.json`)
  const { user, pwd } = req.session
  if (user && user == defaultUser
    && pwd && pwd == defaultPwd
  ) {
    res.sendFile(path.resolve('./index.html'))
  } else {
    res.sendFile(path.resolve('./login.html'))
  }
})

router.post('/form', (req, res) => {
  const { user: defaultUser, pwd: defaultPwd } = require(`../config.json`)
  const { user, pwd } = req.body
  if (user && user == defaultUser
    && pwd && pwd == defaultPwd
  ) {
    req.session.user = defaultUser
    req.session.pwd = defaultPwd
    res.send({ code: 0, msg: 'Authentication succeeded!' })
  } else {
    res.send({ code: 1, msg: 'Authentication failed!' })
  }
})

module.exports = router
