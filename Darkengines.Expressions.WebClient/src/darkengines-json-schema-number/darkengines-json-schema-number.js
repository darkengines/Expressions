
import { PolymerElement, html } from '@polymer/polymer/polymer-element.js';
import '@polymer/paper-input/paper-input.js';

class DarkenginesJsonSchemaNumber extends PolymerElement {
	static get template() {
		return html`<paper-input value="{{value}}" label="[[schema.title]]"></paper-input>`;
	}
	static get properties() {
		return {
			schema: {
				type: Object,
				notify: false,
			},
			value: {
				type: String,
				notify: true
			},
		}
	}
}

window.customElements.define('darkengines-json-schema-number', DarkenginesJsonSchemaNumber);