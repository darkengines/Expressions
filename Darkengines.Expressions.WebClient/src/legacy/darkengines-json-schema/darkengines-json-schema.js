import { PolymerElement, html } from '@polymer/polymer/polymer-element.js';
import '@polymer/paper-input/paper-input.js';
import '../darkengines-json-schema-object/darkengines-json-schema-object.js';
import '../darkengines-json-schema-array/darkengines-json-schema-array.js';
import '../darkengines-json-schema-number/darkengines-json-schema-number.js';
import '../darkengines-json-schema-string/darkengines-json-schema-string.js';

class DarkenginesJsonSchema extends PolymerElement {

	static get map() {
		return {
			object: 'darkengines-json-schema-object',
			array: 'darkengines-json-schema-array',
			string: 'darkengines-json-schema-string',
			integer: 'darkengines-json-schema-number',
		};
	}

	static get template() {
		return html`
<style>
	:host {
		display: block;
	}
</style>`
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
				observer: '_valueChanged',
				notify: true
			},
		}
	}
	_valueChanged(e) {
		if (this.$component) {
			this.$component.set('value', this.value);
		}
	}
	_schemaChanged(e) {
		if (this.schema) {
			this.$component = this.getComponent();
			this.$component.set('value', this.value);
			this.root.append(this.$component);
		}
	}
	getComponent() {
		var type = this.schema.type;
		if (type instanceof Array) {
			type = this.schema.type.find(type => type !== 'null');
		}
		var $element = document.createElement(DarkenginesJsonSchema.map[type]);
		$element.schema = this.schema;
		$element.addEventListener('value-changed', e => {
			this.set(e.detail.path || 'value', e.detail.value);
			this.notifyPath(e.detail.path || 'value');
		});
		return $element;
	}
}

window.customElements.define('darkengines-json-schema', DarkenginesJsonSchema);