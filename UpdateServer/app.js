const express = require('express')
const session = require("express-session")
const path = require('path')
const { dateFormat, readJsonFile, initJsonFile } = require("./utils")

Date.prototype.Format = dateFormat

const app = express()

app.set('env', process.env.NODE_ENV || 'development')
app.set('port', process.env.PORT || 9000);

app.use(session({
  secret: 'wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY',
  cookie: { maxAge: 1000 * 60 * 20 },
  resave: false,
  saveUninitialized: true
}))

app.use(express.json())
app.use(express.urlencoded({ extended: false }))

app.use(function (req, res, next) {
  const paths = ['/form', '/api/app', '/api/download', '/api/test']
  if (paths.includes(req._parsedUrl.pathname)) {
    let obj = readJsonFile(path.resolve(`./config.json`))
    if (obj === null) {
      obj = initJsonFile(path.resolve(`./config.json`), { auth: false, defaultKeys: ['com.borun.updater'], defaultUser: { user: 'root', pwd: '1qaz@WSX' } })
    }
    req.config = obj
  }
  next()
})

app.use('/', require('./routes/index'))
app.use('/api/', require('./routes/api'))

app.use(function (req, res, next) {
  if ('/favicon.ico' !== req.url) {
    var err = new Error(`Resource: '${req.url}' is not found`)
    err.status = 404
    next(err);
  }
})

if (app.get('env') === 'development') {
  app.use(function (err, req, res, next) {
    res.json({
      code: err.status || 500,
      msg: err.message,
      error: err
    })
  })
}

if (app.get('env') === 'production') {
  app.use(function (err, req, res, next) {
    res.json({
      code: err.status || 500,
      msg: err.message || 'error',
      error: {}
    })
  })
}

app.listen(app.get('port'), () => {
  console.log(`env: ${app.get('env')}`)
  console.log(`the server is listening at http://localhost:${app.get('port')}`)
})
