_.extend = function () {
    var deep = (typeof arguments[0] == "boolean" ? arguments[0] == true : false);
    var sindex = (deep ? 2 : 1);
    var obj = arguments[(deep ? 1 : 0)];
    _.each(Array.prototype.slice.call(arguments, 1), function(source) {
        if (source) {
            for (var prop in source) {
                if (obj[prop] != undefined && deep) {
                    if (_.isObject(obj[prop])) {
                        obj[prop] = _.extend(deep, obj[prop], source[prop]);
                    } else {
                        obj[prop] = source[prop];
                    }
                } else {
                    obj[prop] = source[prop];
                }
            }
        }
    });
    return obj;
};