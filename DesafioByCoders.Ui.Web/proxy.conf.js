console.debug("Target api:", process.env["services__desafiobycoders-api__https__0"]);

const target = process.env["services__desafiobycoders-api__https__0"];

const PROXY_CONFIG = {
    "/api": {
        target: target,
        secure: false,
        changeOrigin: true,
        logLevel: "debug"
    }
}

module.exports = PROXY_CONFIG;
