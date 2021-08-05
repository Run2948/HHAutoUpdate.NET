// https://webpack.docschina.org/plugins/terser-webpack-plugin/
const path = require('path');
const CopyWebpackPlugin = require('copy-webpack-plugin')
const TerserPlugin = require("terser-webpack-plugin");
// const NodeExternals = require('webpack-node-externals')

module.exports = {
  mode: 'production',
  entry: './app.js',
  output: {
    filename: 'app.js',
    path: path.resolve(__dirname, 'dist')
  },
  plugins: [
    new CopyWebpackPlugin({
      patterns: [
        // {
        //   from: "uploads",
        //   to: "uploads"
        // },
        {
          from: "login.html",
          to: "login.html"
        },
        {
          from: "index.html",
          to: "index.html"
        },
        {
          from: "web.config",
          to: "web.config"
        }
      ]
    })
  ],
  // externals: [
  //   NodeExternals()
  // ],
  optimization: {
    minimize: true,
    minimizer: [
      // 构建时剥离并删除注释
      new TerserPlugin({
        terserOptions: {
          format: {
            comments: false,
          },
        },
        extractComments: false,
      }),
    ],
  },
  target: 'node' // 这是最关键的
};