import { html, PolymerElement } from '@polymer/polymer/polymer-element.js';
import '../darkengines-expressions-console/darkengines-expressions-console.js';
import '../darkengines-crud/darkengines-crud.js';
import '../darkengines-select-test/darkengines-select-test.js';
import '@polymer/app-route/app-route.js';
import '@polymer/app-route/app-location.js';
import '@polymer/iron-pages/iron-pages.js';

/**
 * @customElement
 * @polymer
 */
class ExpressionsApp extends PolymerElement {
  static get template() {
    return html`
    <style>
      :host {
        display: block;
        height: 100%;
        font-family: verdana
      }
    
      .application {
        height: 100%;
        display: flex;
        flex-direction: column;
      }
    
      .pages {
        height: 100%;
        display: flex;
        flex-direction: column;
      }

      .page {
        height: 100%;
      }
    
      .route {
        font-size: 10px;
        padding: 4px;
      }
    </style>
    <app-location route="{{route}}" use-hash-as-path="true"></app-location>
    <app-route route="{{route}}" pattern="/:page" data="{{routeData}}" tail="{{subroute}}">
    </app-route>
    <div class="application">
      <div class="route">[[route.path]]</div>
      <iron-pages attr-for-selected="page" class="pages" selected="[[routeData.page]]">
        <darkengines-expressions-console page="console" class="page"></darkengines-expressions-console>
        <darkengines-crud page="crud" class="page" route=[[subroute]]></darkengines-crud>
        <darkengines-select-test page="select" class="page"></darkengines-select-test>
      </iron-pages>
    </div>`;
  }
}

window.customElements.define('expressions-app', ExpressionsApp);
