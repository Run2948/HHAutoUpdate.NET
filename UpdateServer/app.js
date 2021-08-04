const express = require('express')

const app = express()

app.set('env', process.env.NODE_ENV || 'development')
app.set('port', process.env.PORT || 9000);

app.use(express.json())
app.use(express.urlencoded({ extended: false }))

app.use('/', require('./routes/index'))
app.use('/api/', require('./routes/api'))

app.use(function (req, res, next) {
  var err = new Error('Not found');
  err.status = 404;
  next(err);
});

if (app.get('env') === 'development') {
  app.use(function (err, req, res, next) {
    res.json({
      code: err.status || 500,
      msg: err.message,
      error: err
    });
  });
}

if (app.get('env') === 'production') {
  app.use(function (err, req, res, next) {
    res.json({
      code: err.status || 500,
      msg: 'error',
      error: {}
    });
  });
}

app.listen(app.get('port'), () => {
  console.log(`env: ${app.get('env')}`)
  console.log(`the server is listening at http://localhost:${app.get('port')
    }`)
})
