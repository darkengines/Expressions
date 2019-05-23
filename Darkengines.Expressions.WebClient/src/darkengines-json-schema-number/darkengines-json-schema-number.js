import { html } from '@polymer/lit-element/lit-element.js';

const numberTemplate = (props) => {
	return html`<paper-input label="${props.schema.title}" .value="${props.value}"></paper-input>`;
}

export { numberTemplate };