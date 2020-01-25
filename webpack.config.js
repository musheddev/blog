// Note this only includes basic configuration for development mode.
// For a more comprehensive configuration check:
// https://github.com/fable-compiler/webpack-config-template

const path = require("path");
const HtmlWebpackPlugin = require("html-webpack-plugin");
const MiniCssExtractPlugin = require("mini-css-extract-plugin");

const babel = {
  plugins: ["@babel/plugin-syntax-dynamic-import"]
};

const isProduction = process.argv.indexOf("-p") >= 0;
console.log(
    "Bundling for " + (isProduction ? "production" : "development") + "..."
);

module.exports = {
  mode: "development",
  entry: "./src/Site.fsproj",
  output: {
    path: path.join(__dirname, "./Site"),
    filename: "[name].bundle.js",
    chunkFilename: "[name].bundle.js",
    publicPath: isProduction ? "/blog/" : "/"
  },
  optimization: {
    minimize: false
  },
  devServer: {
    contentBase: "./Site",
    port: 8080,
    historyApiFallback: true
  },
  module: {
    rules: [
      {
        test: /\.fs(x|proj)?$/,
        use: {
          loader: "fable-loader",
          options: {
            babel
          }
        }
      },
      {
        test: /\.css$/,
        use: [
          isProduction ? MiniCssExtractPlugin.loader : "style-loader",
          { loader: 'css-loader', options: { importLoaders: 1 } },
          // {
          //   loader: 'postcss-loader',
          //   options: {
          //     ident: 'postcss',
          //     plugins: [
          //       require('postcss-import'),
          //       require('tailwindcss'),
          //       require('autoprefixer'),
          //     ],
          //   },
          // },
        ],
      }
    ]
  },
  externals: {
    react: "React",
    "react-dom": "ReactDOM"
  },
  optimization: {
    splitChunks: {
      chunks: "all"
    }
  },
  plugins: isProduction?  [
    new MiniCssExtractPlugin({ filename: "style.css" }),
    new HtmlWebpackPlugin({
      filename: "index.html",
      template: "./src/index.html"
    })
  ] : [
    new MiniCssExtractPlugin({ filename: "style.css" }),
    new HtmlWebpackPlugin({
      filename: "index.html",
      template: "./src/index.html"
    })
  ]
};
