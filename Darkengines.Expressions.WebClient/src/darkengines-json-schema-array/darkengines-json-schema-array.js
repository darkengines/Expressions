import { html } from '@polymer/lit-element/lit-element.js';

const renderItem = (props) => {
	return html`<darkengines-json-schema class="item" .schema="${props.schema}" .value="${props.value}" entityInfos="${props.entityInfos}" .inversePropertyName="${props.inversePropertyName}"></darkengines-json-schema>`;
};

const test = e => {
	e.target.nextElementSibling.classList.add('open');
};

const arrayTemplate = (props) => {
	return html`<style>
	.array {
		display: none;
	}
	.array.open {
		display: block;
	}
	.array .item {
		border: 2px solid black;
	}
	.array .item + .item {
		margin-top: 8px;
	}
	</style><h3>${props.schema ? props.schema.title: null}</h3><div @click=${test}>${props.value ? props.value.length : null} items</div>
	<div class="array">
	${props.value ? props.value.map(value => renderItem({ schema: props.schema.items, value: value, entityInfos: props.entityInfos, inversePropertyName: props.inversePropertyName })) : null}
	</div>`;
};

export { arrayTemplate };