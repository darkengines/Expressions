import { PolymerElement, html } from '@polymer/polymer/polymer-element.js';
import '@polymer/paper-input/paper-input.js';
import '@polymer/app-route/app-route.js';
import { resolve, encode, decode } from '../json-ref';

class DarkenginesCrud extends PolymerElement {
	static get template() {
		return html`
			<app-route route="[[route]]" pattern="/:entityName" data="{{subrouteData}}">
			</app-route>
			[[subrouteData.entityName]]
			[[value.DisplayName]]
			<darkengines-json-schema id="form" schema="[[schema]]" value="{{value}}"></darkengines-json-schema>
		`;
	}
	_subrouteDataChanged(e) {
		if (e && e.entityName) {
			fetch('https://localhost:8080', {
				method: 'POST',
				headers: {
					'Accept': 'application/json',
					'Content-Type': 'application/json'
				},
				body: `Schemas.First(s => s.Key == "${this.subrouteData.entityName}").Value`
			}).then(response => {
				return response.json().then(json => {
					var decoded = resolve(json);
					this.schema = decoded;
				});
			});

			fetch('https://localhost:8080', {
				method: 'POST',
				headers: {
					'Accept': 'application/json',
					'Content-Type': 'application/json'
				},
				body: `Users.First()`
			}).then(response => {
				return response.json().then(json => {
					var decoded = decode(json);
					this.value = decoded;
				});
			});
		}
	}
	static get properties() {
		return {
			schema: {
				type: Object
			},
			value: {
				type: Object,
			},
			route: {
				type: Object
			},
			subrouteData: {
				type: Object,
				observer: '_subrouteDataChanged'
			},
			test: {
				type: String,
				value: 'Mamadou'
			}
		}
	}
	static get observers() {
		return [
			'_valueChanged(value.*)'
		]
	}
	_valueChanged(value) {
		console.log(value);
	}
	ready() {
		super.ready();
	}
}

window.customElements.define('darkengines-crud', DarkenginesCrud);