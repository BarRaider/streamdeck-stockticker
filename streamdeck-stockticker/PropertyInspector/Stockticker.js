function openStockWebsite() {
    if (websocket && (websocket.readyState === 1)) {
        const json = {
            'event': 'openUrl',
            'payload': {
                'url': 'https://github.com/BarRaider/streamdeck-stockticker'
            }
        };
        websocket.send(JSON.stringify(json));
    }
}
