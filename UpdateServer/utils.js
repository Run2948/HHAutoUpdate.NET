const fs = require('fs')
const path = require('path')
var uuid = require('uuid');
const multer = require('multer')

function dateFormat(fmt) {
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
};

function diskStorage(folder) {
  try {
    fs.accessSync(folder)
  } catch (error) {
    fs.mkdirSync(folder)
  }
  const storage = multer.diskStorage({
    destination(req, file, cb) {
      cb(null, folder)
    },
    filename(req, file, cb) {
      cb(null, uuid.v4() + path.extname(file.originalname))
    }
  })
  return multer({ storage: storage })
}

function existsFile(file) {
  return fs.existsSync(file)
}

function readJsonFile(file) {
  const text = fs.readFileSync(file, 'utf-8')
  return JSON.parse(text)
}

function writeJsonFile(file, data) {
  fs.writeFileSync(file, JSON.stringify(data, null, "\t"))
}

function initJsonFile(file, data) {
  if (!existsFile(file)) {
    writeJsonFile(file, data)
  }
}

function renameFile(file, newFile) {
  fs.renameSync(file, newFile)
}

function backupJsonFile(file) {
  if (existsFile(file)) {
    var obj = readJsonFile(file)
    var uuid = obj.ReleaseUrl.replace('./uploads/', '').replace('.zip', '')
    var newFile = file.replace(obj.ApplicationId, `${obj.ApplicationId}_${uuid}`)
    renameFile(file, newFile)
  }
}

module.exports = {
  dateFormat,
  diskStorage,
  existsFile,
  readJsonFile,
  writeJsonFile,
  initJsonFile,
  renameFile,
  backupJsonFile
}