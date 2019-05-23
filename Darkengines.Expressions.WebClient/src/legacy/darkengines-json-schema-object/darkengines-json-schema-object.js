import { PolymerElement, html } from '@polymer/polymer/polymer-element.js';
import '@polymer/paper-input/paper-input.js';
import '../darkengines-json-schema/darkengines-json-schema.js';

class DarkenginesJsonSchemaObject extends PolymerElement {
	static get template() {
		return html`<style>
			.properties {
				padding-left: 16px;
			}
		</style>
		<h4>[[schema.title]]</h4>
		<div id="properties" class="properties">
		</div>`;
	}
	static get properties() {
		return {
			schema: {
				type: Object,
				observer: '_schemaChanged',
				notify: false,
			},
			value: {
				type: Object,
				notify: true,
				observer: '_valueChanged',
			}
		}
	}
	_schemaChanged(e) {
		if (this.schema) {
			var $properties = Object.keys(this.schema.properties).map(propertyKey => {
				var $property = document.createElement('darkengines-json-schema');
				var $title = document.createElement('div');
				$title.textContent = propertyKey;
				$property.className = 'property';
				$property.id = propertyKey;
				$property.addEventListener('value-changed', (e) => {
					var fixedPropertyKey = e.detail.path ? `value.${propertyKey}.${e.detail.path.split('.').slice(1).join('.')}` : `value.${propertyKey}`;
					this.set(fixedPropertyKey, e.detail.value);
					if (e.detail.path) this.notifyPath(fixedPropertyKey);
				});
				$property.set('schema', this.schema.properties[propertyKey]);
				if (this.value) {
					$property.set('value', this.value[propertyKey]);
				}
				var $container = document.createElement('div')
				//$container.append($title);
				$container.append($property);
				return $container;
			});
			$properties.forEach($property => this.$.properties.append($property));
		}
	}
	_valueChanged(v) {
		if (this.schema && this.value) {
			var $properties = this.shadowRoot.querySelectorAll('.properties > div > darkengines-json-schema');
			$properties.forEach($property => $property.set('value', this.value[$property.id]));
		}
	}
	_domChanged(e) {
		
	}
}

window.customElements.define('darkengines-json-schema-object', DarkenginesJsonSchemaObject);