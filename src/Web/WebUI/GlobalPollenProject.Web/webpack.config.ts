import * as webpack from 'webpack'
import * as path from 'path'

const MiniCssExtractPlugin = require("mini-css-extract-plugin");
const CssMinimizerPlugin = require("css-minimizer-webpack-plugin");
const TerserPlugin = require('terser-webpack-plugin');

const config: webpack.Configuration = {
    mode: "production",
    stats: {
        errorDetails: true
    },
    entry: {
        main: "./Scripts/main.ts",
        styles: "./Styles/main.scss"
    },
    devtool: 'inline-source-map',
    output: {
        filename: "[name].bundle.js",
        chunkFilename: "[name].chunk.js",
        path: path.resolve(__dirname, 'wwwroot/scripts/'),
        publicPath: "/scripts/"
    },
    resolve: {
        extensions: ['.ts', '.tsx', '.js'],
        modules: [
            path.resolve('src'),
            'node_modules'
        ],
        alias: {
            "jquery.validation": "jquery-validation/dist/jquery.validate.js"
        }
    },
    optimization: {
        minimize: true,
        minimizer: [new TerserPlugin()],
    },
    externals: {
        jquery: "jQuery"
        //bootstrap: "bootstrap",
    },
    module: {
        rules: [{
                test: /\.tsx?$/,
                loader: 'ts-loader',
                exclude: /node_modules/,
            },
            {
                test: /\.(sass|scss)$/,
                use: [MiniCssExtractPlugin.loader, "css-loader", "sass-loader"]
            },{
                test: /\.(png|svg|jpg|jpeg|gif)$/i,
                type: 'asset/resource'
            }
        ]
    },
    plugins: [
        new MiniCssExtractPlugin({
            filename: './../css/styles.css',
            chunkFilename: "[id].css",
        }),
        new CssMinimizerPlugin()
    ],
}

export default config