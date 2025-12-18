mergeInto(LibraryManager.library, {
  TelegramWebApp_RequestUserDataJson: function (gameObjectNamePtr, callbackMethodNamePtr) {
    var gameObjectName = UTF8ToString(gameObjectNamePtr);
    var callbackMethodName = UTF8ToString(callbackMethodNamePtr);

    var payload = {
      available: false,
      platform: "",
      version: "",
      initData: "",
      initDataUnsafe: null,
      user: null,
      themeParams: null,
      error: ""
    };

    try {
      var tg = (window.Telegram && window.Telegram.WebApp) ? window.Telegram.WebApp : null;
      if (tg) {
        payload.available = true;
        payload.platform = tg.platform || "";
        payload.version = tg.version || "";
        payload.initData = tg.initData || "";
        payload.initDataUnsafe = tg.initDataUnsafe || null;
        payload.user = (tg.initDataUnsafe && tg.initDataUnsafe.user) ? tg.initDataUnsafe.user : null;
        payload.themeParams = tg.themeParams || null;

        if (typeof tg.ready === "function") tg.ready();
        if (typeof tg.expand === "function") tg.expand();
      }
    } catch (e) {
      payload.error = "" + e;
    }

    try {
      var json = JSON.stringify(payload);
      SendMessage(gameObjectName, callbackMethodName, json);
    } catch (e2) {
      SendMessage(gameObjectName, callbackMethodName, "{\"available\":false,\"error\":\"Failed to serialize payload\"}");
    }
  }
  ,
  TelegramWebApp_RequestUserAgent: function (gameObjectNamePtr, callbackMethodNamePtr) {
    var gameObjectName = UTF8ToString(gameObjectNamePtr);
    var callbackMethodName = UTF8ToString(callbackMethodNamePtr);

    var userAgent = "";
    try {
      userAgent = (navigator && navigator.userAgent) ? navigator.userAgent : "";
    } catch (e) {
      userAgent = "";
    }

    try {
      SendMessage(gameObjectName, callbackMethodName, userAgent);
    } catch (e2) {
      // ignore
    }
  }
});
