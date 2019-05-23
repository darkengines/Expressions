import '@polymer/lit-element/lit-element.js';
import { LitElement, html } from '@polymer/lit-element/lit-element.js';
import { objectTemplate } from '../darkengines-json-schema-object/darkengines-json-schema-object.js';
import { arrayTemplate } from '../darkengines-json-schema-array/darkengines-json-schema-array.js';
import { numberTemplate } from '../darkengines-json-schema-number/darkengines-json-schema-number.js';
import { stringTemplate } from '../darkengines-json-schema-string/darkengines-json-schema-string.js';

class DarkenginesJsonSchema extends LitElement {
	constructor() {
		super();
		this.map = {
			'object': objectTemplate,
			'array': arrayTemplate,
			'integer': numberTemplate,
			'string': stringTemplate,
		}
	}
	render() {
		if (this.schema) {
			return html`
				<style>
					:host {
						display: block;
					}
				</style>
				${this.map[this.schema.type instanceof Array ? this.schema.type[1] : this.schema.type]({schema: this.schema, value: this.value, entityInfos: this.entityInfos, inversePropertyName: this.inversePropertyName})}
			`
		} else {
			return html``;
		}
	}
	static get properties() {
		return {
			schema: Object,
			value: Object,
			entityInfos: Object,
			inversePropertyName: String,
			depth: Number
		};
	}
}

window.customElements.define('darkengines-json-schema', DarkenginesJsonSchema);