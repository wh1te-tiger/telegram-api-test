mergeInto(LibraryManager.library, {
    InitializeTelegram: function () {
        if (typeof Telegram !== 'undefined' && Telegram.WebApp) {
            Telegram.WebApp.ready();
            Telegram.WebApp.expand();
            console.log("Telegram WebApp initialized");
        } else {
            console.warn("Telegram WebApp not available");
        }
    },

    SaveData: function (keyPtr, valuePtr) {
        var key = Pointer_stringify(keyPtr);
        var value = Pointer_stringify(valuePtr);
        
        if (typeof Telegram !== 'undefined' && Telegram.WebApp) {
            Telegram.WebApp.CloudStorage.setItem(key, value)
                .then(function(success) {
                    if (success) {
                        unityInstance.SendMessage('TelegramCloudStorage', 'OnDataSaved', key);
                    } else {
                        console.error('Failed to save data for key: ' + key);
                        unityInstance.SendMessage('TelegramCloudStorage', 'OnSaveFailed', key);
                    }
                })
                .catch(function(error) {
                    console.error('Error saving data: ', error);
                    unityInstance.SendMessage('TelegramCloudStorage', 'OnSaveFailed', key);
                });
        } else {
            try {
                localStorage.setItem(key, value);
                unityInstance.SendMessage('TelegramCloudStorage', 'OnDataSaved', key);
                console.log('Saved to localStorage: ' + key + ' = ' + value);
            } catch (e) {
                console.error('LocalStorage error: ', e);
                unityInstance.SendMessage('TelegramCloudStorage', 'OnSaveFailed', key);
            }
        }
    },

    LoadData: function (keyPtr) {
        var key = Pointer_stringify(keyPtr);
        
        if (typeof Telegram !== 'undefined' && Telegram.WebApp) {
            Telegram.WebApp.CloudStorage.getItem(key)
                .then(function(value) {
                    if (value === null) {
                        value = '';
                    }
                    var dataString = key + ':' + value;
                    unityInstance.SendMessage('TelegramCloudStorage', 'OnDataLoaded', dataString);
                })
                .catch(function(error) {
                    console.error('Error loading data: ', error);
                    unityInstance.SendMessage('TelegramCloudStorage', 'OnDataLoaded', key + ':');
                });
        } else {
            try {
                var value = localStorage.getItem(key) || '';
                var dataString = key + ':' + value;
                unityInstance.SendMessage('TelegramCloudStorage', 'OnDataLoaded', dataString);
                console.log('Loaded from localStorage: ' + key + ' = ' + value);
            } catch (e) {
                console.error('LocalStorage error: ', e);
                unityInstance.SendMessage('TelegramCloudStorage', 'OnDataLoaded', key + ':');
            }
        }
    }
});
