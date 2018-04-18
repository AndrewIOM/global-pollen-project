import * as webpack from 'webpack'
import * as path from 'path'

var ExtractTextPlugin = require('extract-text-webpack-plugin');
var OptimiseCssAssets = require('optimize-css-assets-webpack-plugin');

const config: webpack.Configuration = {
    mode: "production",
    devtool: "inline-source-map",
    entry: {
        // Stand-Alone Components
        components: "./Scripts/gpp-components.ts",
        // Individual Pages
        "digitse/app": "./Scripts/Digitise/digitise-app.ts",
        "reference-collection/slide": "./Scripts/ReferenceCollection/view-slide.ts",
        "unknown/identify": "./Scripts/UnknownMaterial/identify-grain.ts",
        "unknown/upload": "./Scripts/UnknownMaterial/upload-grain.ts",
        // Global styles
        styles: "./Styles/main.scss"
    },
    output: {
        filename: '[name].js',
        path: path.resolve(__dirname, 'wwwroot/scripts/')
    },
    resolve: {
        extensions: ['.ts', '.tsx', '.js']
    },
    externals: {
        jquery: "jQuery",
        bootstrap: "bootstrap",
        knockout: "knockout",
        googlemaps: "googlemaps"
    },
    module: {
        rules: [{
                test: /\.tsx?$/,
                loader: 'ts-loader',
                exclude: /node_modules/,
            },
            {
                test: /\.(sass|scss)$/,
                loader: ExtractTextPlugin.extract(['css-loader', 'sass-loader'])
            }
        ]
    },
    plugins: [
        new ExtractTextPlugin({
            filename: './../css/styles.css',
            allChunks: true,
        }),
        new OptimiseCssAssets({
            assetNameRegExp: /\.min\.css$/,
            cssProcessorOptions: { discardComments: { removeAll: true } }
          })
    ]
}

export default config