var defaultReferenceResolver = function (path, root) {
	return path.split('/').reduce(function (memo, piece) {
		if (piece == '#') return root;
		return memo[piece];
	}, null);
};

var resolve = function (root, referenceResolver) {
	referenceResolver = referenceResolver || defaultReferenceResolver;
	var nodes = [{ parent: null, index: null }];
	var dereference = function (path) {
		return referenceResolver(path, root);
	};
	var cache = {};
	while (nodes.length) {
		var node = nodes.splice(0, 1)[0];
		var value = node.parent ? node.parent[node.index] : root;
		if (value instanceof Object) {
			if (value['$ref']) {
				var path = value['$ref'];
				var reference = cache[path] || (cache[path] = dereference(path));
				node.parent[node.index] = reference;
			} else {
				if (value['$id']) {
					cache[value['$id']] = value['$values'] ? value['$values'] : value;
				}
				if (value instanceof Array) {
					value.forEach(function (item, index) {
						nodes.push({ parent: value, index: index });
					});
				} else {
					var keys = Object.keys(value);
					keys.forEach(function (key) {
						nodes.push({ parent: value, index: key });
					});
				}
			}
		}
	}
	return root;
}

var decode = function (node, cache) {
	if (!cache) cache = {};
	if (node instanceof Object) {
		if (node['$ref']) {
			return cache[node['$ref']];
		} else {
			if (node['$id']) {
				cache[node['$id']] = node['$values'] ? node['$values'] : node;
				delete node['$id'];
				if (node['$values']) node = node['$values'];
			}
			if (node instanceof Array) {
				return node.map(item => decode(item, cache))
			} else {
				var result = {};
				var keys = Object.keys(node);
				keys.forEach(function (key) {
					result[key] = decode(node[key], cache);
				});
				return result;
			}
		}
	}
	return node;
}

var encode = function (root) {
	var nodes = [{ parent: null, index: null, parentNode: null }];
	var buildPath = function (node) {
		if (node.parentNode) {
			return buildPath(node.parentNode) + '/' + node.index;
		}
		return '#';
	};
	var references = [];
	while (nodes.length) {
		var node = nodes.splice(0, 1)[0];
		var value = node.parent ? node.parent[node.index] : root;
		if (value instanceof Object) {
			var reference = references.find(function (reference) {
				return (reference.parent ? reference.parent[reference.index] : root) == value;
			});
			if (reference) {
				node.parent[node.index] = { '$ref': buildPath(reference) };
			} else {
				references.push(node);
				if (value instanceof Array) {
					value.forEach(function (item, index) {
						nodes.push({ parent: value, index: index, parentNode: node });
					});
				} else {
					var keys = Object.keys(value);
					keys.forEach(function (key) {
						nodes.push({ parent: value, index: key, parentNode: node });
					});
				}
			}
		}
	}
	return root;
};

export { resolve, encode, decode };