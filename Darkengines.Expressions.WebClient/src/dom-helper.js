var classes = (classes) => {
	return `${Object.keys(classes).filter(className => classes[className]).join(' ')}`;
}

export { classes };