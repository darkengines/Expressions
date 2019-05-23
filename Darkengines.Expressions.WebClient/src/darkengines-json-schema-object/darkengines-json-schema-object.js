import { html } from '@polymer/lit-element/lit-element.js';

const renderProperty = (props) => {
	return html`<darkengines-json-schema class="property" .schema="${props.schema}" .value="${props.value}" .entityInfos="${props.entityInfos}" .inversePropertyName="${props.inversePropertyName}"></darkengines-json-schema>`;
};

const objectTemplate = (props) => {
	var propertiesKeys = Object.keys(props.schema.properties);
	return html`<style>
		.property {
			padding-left: 16px;
		}
	</style>
	<h3>${props.schema ? props.schema.title: null}</h3>
	${props.value && props.schema ? propertiesKeys.filter(key => key !== props.inversePropertyName).map(key => {
		var entityInfo = props.entityInfos && props.entityInfos[props.schema.$id];
		var navigation = entityInfo && entityInfo.Navigations[key];
		var inversePropertyName = navigation && navigation.InversePropertyName;
		return renderProperty({ 
			schema: props.schema.properties[key], 
			value: props.value[key], 
			inversePropertyName: inversePropertyName,
			entityInfos: props.entityInfos
		});
	}) : null}`;
};

export { objectTemplate };