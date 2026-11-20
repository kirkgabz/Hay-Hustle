// Hay Hustle - browser helpers (used by WebInput.cs).
// Shows a real HTML text box over the game so phones open their on-screen keyboard.
mergeInto(LibraryManager.library, {
  HH_InputOpen: function (titlePtr, valuePtr, kind, maxLength) {
    var title = UTF8ToString(titlePtr);
    var value = UTF8ToString(valuePtr);
    window.hhInputState = 0;
    window.hhInputValue = value;
    var old = document.getElementById("hh-input");
    if (old) old.parentNode.removeChild(old);

    var wrap = document.createElement("div");
    wrap.id = "hh-input";
    wrap.style.cssText = "position:fixed;left:0;top:0;right:0;bottom:0;background:rgba(0,0,0,0.6);display:flex;" +
      "align-items:flex-start;justify-content:center;z-index:9999;padding-top:6vh;font-family:Arial,sans-serif;";
    var form = document.createElement("form");
    form.style.cssText = "background:#1b1610;border-top:3px solid #f2bf40;color:#fff;padding:16px;border-radius:10px;" +
      "width:min(92vw,520px);box-sizing:border-box;box-shadow:0 8px 30px rgba(0,0,0,0.5);";
    var label = document.createElement("div");
    label.textContent = title;
    label.style.cssText = "font-size:18px;font-weight:bold;margin-bottom:10px;";
    var input = document.createElement("input");
    input.type = kind == 1 ? "password" : (kind == 2 ? "email" : "text");
    input.value = value;
    if (maxLength > 0) input.maxLength = maxLength;
    input.autocomplete = kind == 1 ? "current-password" : (kind == 2 ? "email" : "off");
    input.setAttribute("autocapitalize", kind == 0 ? "sentences" : "none");
    input.setAttribute("autocorrect", "off");
    input.spellcheck = false;
    input.style.cssText = "width:100%;font-size:20px;padding:10px;box-sizing:border-box;border-radius:6px;border:0;" +
      "background:#f3eedc;color:#1a140b;";
    var row = document.createElement("div");
    row.style.cssText = "display:flex;gap:10px;margin-top:12px;";
    function makeButton(text, bg) {
      var b = document.createElement("button");
      b.type = "button";
      b.textContent = text;
      b.style.cssText = "flex:1;font-size:18px;font-weight:bold;padding:12px;border:0;border-radius:6px;color:#fff;background:" + bg + ";";
      return b;
    }
    var cancel = makeButton("Cancel", "#555");
    var ok = makeButton("OK", "#4a6a2f");
    function close(state) {
      window.hhInputValue = input.value;
      window.hhInputState = state;
      if (wrap.parentNode) wrap.parentNode.removeChild(wrap);
    }
    ok.onclick = function () { close(1); };
    cancel.onclick = function () { close(2); };
    form.onsubmit = function (e) { e.preventDefault(); close(1); };
    row.appendChild(cancel);
    row.appendChild(ok);
    form.appendChild(label);
    form.appendChild(input);
    form.appendChild(row);
    wrap.appendChild(form);
    document.body.appendChild(wrap);
    setTimeout(function () { input.focus(); }, 60);
  },

  HH_InputState: function () {
    return (window.hhInputState === undefined || window.hhInputState === null) ? -1 : window.hhInputState;
  },

  HH_InputValue: function () {
    var s = window.hhInputValue || "";
    var size = lengthBytesUTF8(s) + 1;
    var buffer = _malloc(size);
    stringToUTF8(s, buffer, size);
    return buffer;
  },

  HH_InputReset: function () {
    window.hhInputState = -1;
  }
});
