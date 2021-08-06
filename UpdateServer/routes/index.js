const express = require('express')
const router = express.Router()
const path = require('path')

router.get('/', (req, res) => {
  res.sendStatus(200)
})

router.get('/form', (req, res) => {
  const { defaultUser } = req.config
  const { user, pwd } = req.session
  if (user && user == defaultUser.user
    && pwd && pwd == defaultUser.pwd
  ) {
    res.sendFile(path.resolve('./index.html'))
  } else {
    res.sendFile(path.resolve('./login.html'))
  }
})

router.post('/form', (req, res) => {
  const { defaultUser } = req.config
  const { user, pwd } = req.body
  if (user && user == defaultUser.user
    && pwd && pwd == defaultUser.pwd
  ) {
    req.session.user = defaultUser.user
    req.session.pwd = defaultUser.pwd
    res.send({ code: 0, msg: 'Authentication succeeded!' })
  } else {
    res.send({ code: 1, msg: 'Authentication failed!' })
  }
})

module.exports = router
