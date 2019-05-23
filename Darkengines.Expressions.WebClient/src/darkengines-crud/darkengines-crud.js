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
			<darkengines-json-schema id="form" schema="[[schema]]" value="{{value}}" entity-infos="[[entityInfos]]"></darkengines-json-schema>
		`;
	}
	_subrouteDataChanged(e) {
		if (e && e.entityName) {
			fetch('http://192.168.1.2:8080', {
				method: 'POST',
				headers: {
					'Accept': 'application/json',
					'Content-Type': 'application/json'
				},
				body: `RootSchema`
			}).then(response => {
				return response.json().then(json => {
					var decoded = resolve(json);
					this.schema = decoded.properties[e.entityName].oneOf[1];
					
					var query = Object.keys(this.schema.properties).reduce((r, p) => {
						var type = this.schema.properties[p].type;
						if (type instanceof Array) type = type[1];
						if (type == 'array') r = `${r}.Include(x => x.${p})`;
						return r;
					}, 'Users');
					query += '.FirstOrDefault()';

					fetch('http://192.168.1.2:8080', {
						method: 'POST',
						headers: {
							'Accept': 'application/json',
							'Content-Type': 'application/json'
						},
						body: query
					}).then(response => {
						return response.json().then(json => {
							var decoded = decode(json);
							this.value = decoded;
						});
					});

				});
			});

			fetch('http://192.168.1.2:8080', {
				method: 'POST',
				headers: {
					'Accept': 'application/json',
					'Content-Type': 'application/json'
				},
				body: `EntityInfos`
			}).then(response => {
				return response.json().then(json => {
					var decoded = decode(json);
					this.entityInfos = decoded;
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
			entityInfos: {
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