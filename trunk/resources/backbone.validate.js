_.extend = function() {
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
Backbone = _.extend(Backbone, {
    ErrorMessages: {},
    Language: 'en',
    TranslateValidationError: function(errorMessage) {
        var props = ((Backbone.Language) + '.' + errorMessage).split('.');
        var obj = Backbone.ErrorMessages;
        var ret = errorMessage;
        for (var x = 0; x < props.length; x++) {
            if (obj[props[x]] != undefined) {
                obj = obj[props[x]];
            } else {
                obj = null;
                x = props.length;
            }
        }
        if (obj != null) {
            ret = obj;
        }
        return ret;
    },
    ShowModelError: function(model) {
        var erDialog = $('#Backbone_Error_Dialog');
        if (erDialog.length == 0) {
            erDialog = $('<div id="Backbone_Error_Dialog" class="dialog error"></div>');
            $(document.body).append(erDialog);
        }
        erDialog.html('');
        var ul = $('<ul></ul>');
        erDialog.append(ul);
        for (var x = 0; x < model.errors.length; x++) {
            ul.append('<li class="' + model.errors[x].field + '"><span class="error_field_name">' + model.errors[x].field + '</span><span class="error_field_message">' + model.errors[x].error + '</span></li>');
        }
        erDialog.show();
    },
    Model: _.extend(Backbone.Model, {
        prototype: _.extend(Backbone.Model.prototype, {
            get: function(attr) {
                if (this.LazyLoadAttributes != undefined) {
                    for (var x = 0; x < this.LazyLoadAttributes.length; x++) {
                        if (this.LazyLoadAttributes[x] == attr) {
                            if (this.attributes[attr] != null) {
                                if (this.attributes[attr] instanceof Array) {
                                    if (this.attributes[attr].length > 0) {
                                        if (!(this.attributes[attr][0].isLoaded == undefined ? false : this.attributes[attr][0].isLoaded)) {
                                            for (var x = 0; x < this.attributes[attr].length; x++) {
                                                this.attributes[attr][x].fetch();
                                                this.attributes[attr][x].isLoaded = true;
                                            }
                                        }
                                    }
                                } else {
                                    if (!(this.attributes[attr].isLoaded == undefined ? false : this.attributes[attr].isLoaded)) {
                                        this.attributes[attr].fetch();
                                        this.attributes[attr].isLoaded = true;
                                    }
                                }
                            }
                            x = this.LazyLoadAttributes.length;
                        }
                    }
                }
                return this.attributes[attr];
            },
            syncSave: function(attrs, options) {
                if (!options) { options = {}; }
                options = _.extend(options, { async: false });
                return this.save(attrs, options);
            },
            syncDestroy: function(options) {
                if (!options) { options = {}; }
                options = _.extend(options, { async: false });
                return this.destroy(options);
            }
        })
    })
});