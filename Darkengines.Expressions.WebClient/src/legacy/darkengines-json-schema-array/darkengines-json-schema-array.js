
import { PolymerElement, html } from '@polymer/polymer/polymer-element.js';
import '@polymer/paper-input/paper-input.js';

class DarkenginesJsonSchemaArray extends PolymerElement {
	static get template() {
		return html`
<style>
	:host {
		display: block;
	}
	.items {
		padding-left: 16px;
	}
	.item {
		border: solid 1px black;
		margin: 4px;
	}
</style>
<h4>[[schema.title]]</h4>
<div class="items">
	<dom-repeat items="{{value}}">
		<template>
			<darkengines-json-schema class="item" schema=[[schema.items]] value={{item}}></darkengines-json-schema>
		</template>
	</dom-repeat>
	<div on-click="addItem">
		Add
	</div>
</div>`;
	}
	addItem() {
		this.push('value', {});
	}
	static get properties() {
		return {
			schema: {
				type: Object,
				notify: false,
			},
			value: {
				type: Array,
				notify: true,
				value: []
			}
		}
	}
}

window.customElements.define('darkengines-json-schema-array', DarkenginesJsonSchemaArray);