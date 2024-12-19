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
    setExcloudSettings("none");
    setAPITokenSettings("none");
    setFinnhubSettings("none");

    setSingleModeSettings("");
    setMultipleModeSettings("none");
    if (payload['stockProvider'] == 5) {
        setFinnhubSettings("");
        setAPITokenSettings("");
    }
    else  if (payload['stockProvider'] == 1) {
        setExcloudSettings("");
        setAPITokenSettings("");
    }

    if (payload["modeMultiple"]) {
        setSingleModeSettings("none");
        setMultipleModeSettings("");
    }
}

function setExcloudSettings(displayValue) {
    var dvExcloudSettings = document.getElementById('dvExcloudSettings');
    dvExcloudSettings.style.display = displayValue;
}

function setAPITokenSettings(displayValue) {
    var dvAPIToken = document.getElementById('dvAPIToken');
    dvAPIToken.style.display = displayValue;
}

function setFinnhubSettings(displayValue) {
    var dvFinnhubSettings = document.getElementById('dvFinnhubSettings');
    dvFinnhubSettings.style.display = displayValue;
}

function setSingleModeSettings(displayValue) {
    var dvSingleModeSettings = document.getElementById('dvSingleModeSettings');
    dvSingleModeSettings.style.display = displayValue;
}

function setMultipleModeSettings(displayValue) {
    var dvMultipleModeSettings = document.getElementById('dvMultipleModeSettings');
    dvMultipleModeSettings.style.display = displayValue;
}

function openIexSupport() {
    if (websocket && (websocket.readyState === 1)) {
        const json = {
            'event': 'openUrl',
            'payload': {
                'url': 'https://barraider.com/iexsupport'
            }
        };
        websocket.send(JSON.stringify(json));
    }
}

function openFinSupport() {
    if (websocket && (websocket.readyState === 1)) {
        const json = {
            'event': 'openUrl',
            'payload': {
                'url': 'https://barraider.com/finsupport'
            }
        };
        websocket.send(JSON.stringify(json));
    }
}
