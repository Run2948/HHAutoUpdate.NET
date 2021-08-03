const express = require('express')
const router = express.Router()
const path = require('path')

router.get('/', async (req, res) => {
  await res.sendStatus(200)
})

router.get('/form', async (req, res) => {
  await res.sendFile(path.resolve('./index.html'))
})

module.exports = router
