(function () {
    var wrapError = function (model, options) {
        var error = options.error;
        options.error = function (resp) {
            if (error) error(model, resp, options);
            model.trigger('error', model, resp, options);
        };
    };
    Backbone = _.extend(Backbone, {
        ErrorMessages: {},
        Language: 'en',
        TranslateValidationError: function (errorMessage) {
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
        ShowModelError: function (model) {
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
        }
    });

    Backbone.Model.prototype.get = function (attr) {
        if (this.LazyLoadAttributes != undefined) {
            for (var x = 0; x < this.LazyLoadAttributes.length; x++) {
                if (this.LazyLoadAttributes[x] == attr) {
                    if (this.attributes[attr] != null) {
                        if (this.attributes[attr] instanceof Backbone.Collection) {
                            if (this.attributes[attr].length > 0) {
                                if (!(this.attributes[attr].at(0).isLoaded == undefined ? false : this.attributes[attr].at(0).isLoaded) && _.keys(this.attributes[attr].at(0).attributes).length == 1) {
                                    for (var x = 0; x < this.attributes[attr].length; x++) {
                                        this.attributes[attr].at(x).fetch({ async: false });
                                        this.attributes[attr].at(x).isLoaded = true;
                                        this.attributes[attr].at(x)._previousAttributes = this.attributes[attr].at(x).attributes;
                                    }
                                }
                            }
                        } else if (this.attributes[attr] instanceof Array) {
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
    };
    //added in sync Save function to specify synchronous communication
    Backbone.Model.prototype.syncSave = function (attrs, options) {
        if (!options) { options = {}; }
        options = _.extend(options, { async: false });
        return this.save(attrs, options);
    };
    //added in sync destroy function to specify synchronous communication
    Backbone.Model.prototype.syncDestroy = function (options) {
        if (!options) { options = {}; }
        options = _.extend(options, { async: false });
        return this.destroy(options);
    };
    eval('Backbone.Model.prototype._baseSave = ' + Backbone.Model.prototype.save.toString());
    eval('Backbone.Model.prototype._destroy = ' + Backbone.Model.prototype.destroy.toString());
    Backbone.Model.prototype._save = function (key, val, options) {
        if (key == null || typeof key === 'object') {
            attrs = key;
            options = val;
        } else {
            (attrs = {})[key] = val;
        }
        options = _.extend({ validate: true }, options);
        this._baseSave(attrs, _.extend({}, {
            originalOptions: _.clone(options),
            originalSuccess: options.success,
            success: function (model, response, options) {
                model._origAttributes = _.clone(model.attributes);
                if (options.originalSuccess != undefined) {
                    options.originalSuccess(model, response, options.originalOptions);
                }
            }
        }));
    };
    Backbone.Model.prototype.save = function (key, val, options) { this._save(key, val, options); };
    Backbone.Model.prototype.changedAttributes = function (diff) {
        this._origAttributes = (this._origAttributes == undefined ? {} : this._origAttributes);
        if (!diff) return this.hasChanged() ? _.clone(this.changed) : false;
        var val, changed = false;
        for (var attr in diff) {
            if (_.isEqual(this._origAttributes[attr], (val = diff[attr]))) continue;
            (changed || (changed = {}))[attr] = val;
        }
        return changed;
    }
}).call(this);