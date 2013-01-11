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
    Collection: _.extend(Backbone.Collection, {
        prototype: _.extend(Backbone.Collection.prototype, {
            update: function(models, options) {
                var model, i, l, existing;
                var add = [], remove = [], modelMap = {};
                var idAttr = this.model.prototype.idAttribute;
                options = _.extend({ add: true, merge: true, remove: true, fetch: true }, options);
                if (options.parse) models = this.parse(models);
                if (!_.isArray(models)) models = models ? [models] : [];
                if (options.add && !options.remove) return this.add(models, options);
                for (i = 0, l = models.length; i < l; i++) {
                    model = models[i];
                    existing = this.get(model.id || model.cid || model[idAttr]);
                    if (options.remove && existing) modelMap[existing.cid] = true;
                    if ((options.add && !existing) || (options.merge && existing)) {
                        add.push(model);
                    }
                }
                if (options.remove) {
                    for (i = 0, l = this.models.length; i < l; i++) {
                        model = this.models[i];
                        if (!modelMap[model.cid]) remove.push(model);
                    }
                }
                if (remove.length) this.remove(remove, options);
                if (add.length) this.add(add, options);
                return this;
            },
            reset: function(models, options) {
                options || (options = {});
                if (options.parse) models = this.parse(models);
                for (var i = 0, l = this.models.length; i < l; i++) {
                    this._removeReference(this.models[i]);
                }
                options.previousModels = this.models;
                this._reset();
                if (models) this.add(models, _.extend({ silent: true, fetch: true }, options));
                if (!options.silent) this.trigger('reset', this, options);
                return this;
            },
            fetch: function(options) {
                options = options ? _.clone(options) : {};
                options = _.extend(options, { fetch: true });
                if (options.parse === void 0) options.parse = true;
                var collection = this;
                var success = options.success;
                options.success = function(resp, status, xhr) {
                    var method = options.update ? 'update' : 'reset';
                    collection[method](resp, options);
                    if (success) success(collection, resp, options);
                };
                return this.sync('read', this, options);
            }
        })
    }),
    Model: _.extend(Backbone.Model, {
        prototype: _.extend(Backbone.Model.prototype, {
            set: function(key, val, options) {
                if (this._changedFields == undefined) {
                    this._changedFields = new Array();
                }
                var attr, attrs;
                if (key == null) return this;
                if (_.isObject(key)) {
                    attrs = key;
                    options = val;
                } else {
                    (attrs = {})[key] = val;
                }
                var silent = options && options.silent;
                var fetch = options && options.fetch;
                var unset = options && options.unset;
                if (!this._validate(attrs, options)) return false;
                if (this.idAttribute in attrs) this.id = attrs[this.idAttribute];
                var now = this.attributes;
                for (attr in attrs) {
                    val = attrs[attr];
                    if (unset) {
                        delete now[attr];
                        if (!fetch) {
                            this._changedFields.push(attr);
                        }
                    } else {
                        now[attr] = val;
                        if (!fetch && attr != 'id') {
                            if (this.previousAttributes() != undefined) {
                                if (this.previousAttributes()[attr] != val) {
                                    this._changedFields.push(attr);
                                }
                            }
                        }
                    }
                    this._changes.push(attr, val);
                }
                this._hasComputed = false;
                if (fetch) this._changedFields = new Array();
                if (!silent) this.change(options);
                return this;
            },
            fetch: function(options) {
                options = options ? _.clone(options) : {};
                options.fetch = true;
                if (options.parse === void 0) options.parse = true;
                var model = this;
                var success = options.success;
                options.success = function(resp, status, xhr) {
                    if (!model.set(model.parse(resp), options)) return false;
                    if (success) success(model, resp, options);
                };
                return this.sync('read', this, options);
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
            _save: function(key, val, options) {
                var attrs, current, done;
                if (key == null || _.isObject(key)) {
                    attrs = key;
                    options = val;
                } else if (key != null) {
                    (attrs = {})[key] = val;
                }
                options = options ? _.clone(options) : {};
                if (options.wait) {
                    if (attrs && !this._validate(attrs, options)) return false;
                    current = _.clone(this.attributes);
                }
                var silentOptions = _.extend({}, options, { silent: true });
                if (attrs && !this.set(attrs, options.wait ? silentOptions : options)) {
                    return false;
                }
                if (!attrs && !this._validate(null, options)) return false;
                var model = this;
                var success = options.success;
                options.success = function(resp, status, xhr) {
                    done = true;
                    var serverAttrs = model.parse(resp);
                    if (options.wait) serverAttrs = _.extend(attrs || {}, serverAttrs);
                    if (!model.set(serverAttrs, options)) return false;
                    if (success) success(model, resp, options);
                };
                var method = this.isNew() ? 'create' : (options.patch ? 'patch' : 'update');
                if (method == 'patch') options.attrs = attrs;
                var xhr = this.sync(method, this, options);
                if (!done && options.wait) {
                    this.clear(silentOptions);
                    this.set(current, silentOptions);
                }
                return xhr;
            },
            _destroy: function(options) {
                options = options ? _.clone(options) : {};
                var model = this;
                var success = options.success;
                var destroy = function() {
                    model.trigger('destroy', model, model.collection, options);
                };
                options.success = function(resp) {
                    if (options.wait || model.isNew()) destroy();
                    if (success) success(model, resp, options);
                };
                if (this.isNew()) {
                    options.success();
                    return false;
                }
                var xhr = this.sync('delete', this, options);
                if (!options.wait) destroy();
                return xhr;
            }
        })
    })
});