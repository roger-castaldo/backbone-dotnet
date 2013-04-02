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
                                        if (!(this.attributes[attr][0].isLoaded == undefined ? false : this.attributes[attr][0].isLoaded) && _.keys(this.attributes[attr][0].attributes).length == 1) {
                                            for (var x = 0; x < this.attributes[attr].length; x++) {
                                                this.attributes[attr][x].fetch({ async: false });
                                                this.attributes[attr][x].isLoaded = true;
                                                this.attributes[attr][x]._previousAttributes = this.attributes[attr][x].attributes;
                                            }
                                        }
                                    }
                                } else {
                                    if (!(this.attributes[attr].isLoaded == undefined ? false : this.attributes[attr].isLoaded) && _.keys(this.attributes[attr].attributes).length == 1) {
                                        this.attributes[attr].fetch({ async: false });
                                        this.attributes[attr].isLoaded = true;
                                        this.attributes[attr]._previousAttributes = this.attributes[attr].attributes;
                                    }
                                }
                                this._previousAttributes[attr] = this.attributes[attr];
                            }
                            x = this.LazyLoadAttributes.length;
                        }
                    }
                }
                return this.attributes[attr];
            },
            toJSON: function() {
                var attrs = {};
                var keys = _.keys(this.attributes);
                for (x in keys) {
                    var key = keys[x];
                    if (!_.isEqual(this.attributes[key], this._previousAttributes[key])) {
                        attrs[key] = this.attributes[key];
                    }
                }
                return attrs;
            },
            hasChanged: function() {
                var keys = _.keys(this.attributes);
                for (x in keys) {
                    var key = keys[x];
                    if (!_.isEqual(this.attributes[key], this._previousAttributes[key])) {
                        return true;
                    }
                }
                var keys = _.keys(this._previousAttributes);
                for (x in keys) {
                    var key = keys[x];
                    if (!_.isEqual(this.attributes[key], this._previousAttributes[key])) {
                        return true;
                    }
                }
                return false;
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
            },
            _save: Backbone.Model.prototype.save,
            _delete:Backbone.Model.prototype.delete
        })
    })
});