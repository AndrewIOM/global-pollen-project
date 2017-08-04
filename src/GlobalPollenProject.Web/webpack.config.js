const webpack = require('webpack')
const path = require('path')
const ExtractTextPlugin = require("extract-text-webpack-plugin");

var config = {
  context: __dirname + '/temp',
  entry: {
    styles: [ './../Styles/main.scss' ]
  },
  output: {
    path: __dirname + '/wwwroot/js',
    filename: '[name].bundle.js',
    libraryTarget: "var"
  },
  target: "web",
  devtool: "source-map",
  module: {
    rules: [{
      test: /\.js$/,
      include: path.resolve(__dirname, 'src'),
      use: [{
        loader: 'babel-loader',
        options: {
          presets: [
            ['es2015', { modules: false }]
          ]
        }
      }]
    },
    {
      test: /\.scss$/,
        use: ExtractTextPlugin.extract({
            use: [{
                loader: "css-loader"
            }, {
                loader: "sass-loader"
            }],
            fallback: "style-loader"
        })
    },
    { test: /\.png$/, loader: 'ignore-loader' }]
  },
  plugins: [
    new ExtractTextPlugin({
        filename: "./../css/[name].css",
        allChunks: true
    })
  ]
};

module.exports = config;
