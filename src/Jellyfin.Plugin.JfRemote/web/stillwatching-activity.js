/*
 * stillwatching-activity.js - makes REMOTE-CONTROL actions (and any real user
 * activity) reset the web player's "Are you still watching?" timer.
 *
 * Stock behavior: the prompt fires after N autoplayed episodes OR long idle,
 * where "idle" only resets on LOCAL mouse/key/touch input. Commands sent from
 * a remote (pause, seek, episode changes over the session WebSocket) never
 * touch that idle clock, and the episode counter only resets on a fresh play
 * session or on confirming the dialog.
 *
 * This script:
 *  1. Listens on the player's session WebSocket: any Play / Playstate /
 *     GeneralCommand message calls the input manager's notify() - the exact
 *     reset local input performs.
 *  2. Wraps the built-in stillWatching plugin's intercept(): at each episode
 *     transition, if there was ANY activity since the previous episode began
 *     (idleTime < elapsed), the prompt session/episode counter is reset.
 *     Locally clicking "Start now" therefore also resets it.
 * Both hooks locate modules by code signature, not build ids, and no-op
 * safely if the player internals change.
 */
(function () {
  'use strict';

  var req = null;
  function getReq() {
    if (req) return req;
    try {
      (self.webpackChunk = self.webpackChunk || []).push(
        [[Date.now() + Math.random()], {}, function (r) { req = r; }]);
    } catch (e) { /* no-op */ }
    return req;
  }
  function findModule(pred) {
    var r = getReq();
    if (!r || !r.m) return null;
    for (var id in r.m) {
      if (pred(String(r.m[id]))) {
        try { return r(id); } catch (e) { /* not loadable yet */ }
      }
    }
    return null;
  }

  var input = null;
  function inputMgr() {
    if (input) return input;
    var m = findModule(function (s) {
      return s.indexOf('notifyMouseMove') >= 0 && s.indexOf('idleTime') >= 0;
    });
    if (m && typeof m.notify === 'function' && typeof m.idleTime === 'function') input = m;
    return input;
  }

  // ---- 1) remote commands reset the idle clock --------------------------
  var REMOTE_TYPES = { Play: 1, Playstate: 1, GeneralCommand: 1 };
  function onSocketMessage(ev) {
    try {
      var m = JSON.parse(ev.data);
      if (m && REMOTE_TYPES[m.MessageType]) {
        var im = inputMgr();
        if (im) im.notify();
      }
    } catch (e) { /* not JSON / not ours */ }
  }
  try {
    var Prev = window.WebSocket;
    if (Prev && !Prev.__swActivityPatched) {
      var Wrapped = function (url, protocols) {
        var ws = protocols !== undefined ? new Prev(url, protocols) : new Prev(url);
        try {
          if (typeof url === 'string' && url.indexOf('/socket') >= 0) {
            ws.addEventListener('message', onSocketMessage);
          }
        } catch (e) { /* no-op */ }
        return ws;
      };
      Wrapped.prototype = Prev.prototype;
      Wrapped.CONNECTING = Prev.CONNECTING;
      Wrapped.OPEN = Prev.OPEN;
      Wrapped.CLOSING = Prev.CLOSING;
      Wrapped.CLOSED = Prev.CLOSED;
      Wrapped.__swActivityPatched = true;
      window.WebSocket = Wrapped;
    }
  } catch (e) { /* no-op */ }

  // ---- 2) interaction resets the still-watching episode counter --------
  var patched = false;
  function patchStillWatching() {
    if (patched) return true;
    var m = findModule(function (s) {
      return s.indexOf('resetSession') >= 0 && s.indexOf('playedItems') >= 0;
    });
    var Cls = m && m.default;
    if (!Cls || !Cls.prototype || typeof Cls.prototype.intercept !== 'function') return false;
    var orig = Cls.prototype.intercept;
    Cls.prototype.intercept = function (opts) {
      try {
        var now = Date.now();
        if (!opts || !opts.isFirstItem) {
          var since = now - (this.__swLastIntercept || now);
          var im = inputMgr();
          if (im && im.idleTime() < since && typeof this.resetSession === 'function') {
            this.resetSession();       // user (local or remote) was active - start over
          }
        }
        this.__swLastIntercept = now;
      } catch (e) { /* never break playback */ }
      return orig.apply(this, arguments);
    };
    patched = true;
    console.debug('[stillwatching-activity] intercept patched');
    return true;
  }
  var tries = 0;
  var t = setInterval(function () {
    if (patchStillWatching() || ++tries > 60) clearInterval(t);
  }, 3000);
  patchStillWatching();
})();
