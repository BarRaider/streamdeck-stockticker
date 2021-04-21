document.addEventListener('websocketCreate', function () {
    console.log("Websocket created!");
    showHideSettings(actionInfo.payload.settings);

    websocket.addEventListener('message', function (event) {
        console.log("Got message event!");

        // Received message from Stream Deck
        var jsonObj = JSON.parse(event.data);

        if (jsonObj.event === 'sendToPropertyInspector') {
            var payload = jsonObj.payload;
            showHideSettings(payload);
        }
        else if (jsonObj.event === 'didReceiveSettings') {
            var payload = jsonObj.payload;
            showHideSettings(payload.settings);
        }
    });
});

function showHideSettings(payload) {
    console.log("Show Hide Settings Called");
    setAllSettings("none");
    if (payload['apiToken'] && payload['apiToken'].length > 0) {
        setAllSettings("");
    }
}

function setAllSettings(displayValue) {
    var dvAllSettings = document.getElementById('dvAllSettings');
    dvAllSettings.style.display = displayValue;
}

function openAPISite() {
    if (websocket && (websocket.readyState === 1)) {
        const json = {
            'event': 'openUrl',
            'payload': {
                'url': 'https://buz.bz/Iyhj'
            }
        };
        websocket.send(JSON.stringify(json));
    }
}
