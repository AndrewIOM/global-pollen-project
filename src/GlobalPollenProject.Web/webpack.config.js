const webpack = require('webpack')
const path = require('path')

var config = {
  context: __dirname + '/temp',
  entry: {
    taxonomy: './Scripts/taxonomy'
  },
  output: {
    path: __dirname + '/wwwroot/js',
    filename: '[name].bundle.js',
    libraryTarget: "commonjs2"
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
      use: [
        'style-loader',
        'css-loader',
        'sass-loader'
      ]
    }]
  }
};

module.exports = config;
