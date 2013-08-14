//Added in additional code to handle error messages components
Backbone.ErrorMessages = {};

//set default error message language
Backbone.Language = 'en';

//called to translate an error object to a different language if available, designed to handle multi-language support
Backbone.TranslateValidationError = function(errorMessage) {
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
};

//called to show a model error through an error dialog popup
Backbone.ShowModelError = function(model) {
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
};
    
Backbone.Model: _.extend(Backbone.Model, {
    prototype: _.extend(Backbone.Model.prototype, {
        //replacing get function to handle lazy loading properties
        get: function(attr) {
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
        //replacing toJSON to only return values that are different
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
        //replacing changed function to only check for changed properly
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
        
        //added in sync Save function to specify synchronous communication
        syncSave: function(attrs, options) {
            if (!options) { options = {}; }
            options = _.extend(options, { async: false });
            return this.save(attrs, options);
        },
        
        //added in sync destroy function to specify synchronous communication
        syncDestroy: function(options) {
            if (!options) { options = {}; }
            options = _.extend(options, { async: false });
            return this.destroy(options);
        },
        
        _save: function(key, val, options) {
            var attrs, method, xhr, attributes = this.attributes;

            // Handle both `"key", value` and `{key: value}` -style arguments.
            if (key == null || typeof key === 'object') {
                attrs = key;
                options = val;
            } else {
                (attrs = {})[key] = val;
            }

            // If we're not waiting and attributes exist, save acts as `set(attr).save(null, opts)`.
            if (attrs && (!options || !options.wait) && !this.set(attrs, options)) return false;

            options = _.extend({ validate: true }, options);

            // Do not persist invalid models.
            if (!this._validate(attrs, options)) return false;

            // Set temporary attributes if `{wait: true}`.
            if (attrs && options.wait) {
                this.attributes = _.extend({}, attributes, attrs);
            }

            // After a successful server-side save, the client is (optionally)
            // updated with the server-side state.
            if (options.parse === void 0) options.parse = true;
            var model = this;
            var success = options.success;
            options.success = function(resp) {
                // Ensure attributes are restored during synchronous saves.
                model.attributes = attributes;
                var serverAttrs = model.parse(resp, options);
                if (options.wait) serverAttrs = _.extend(attrs || {}, serverAttrs);
                if (_.isObject(serverAttrs) && !model.set(serverAttrs, options)) {
                    return false;
                }
                if (success) success(model, resp, options);
                model.trigger('sync', model, resp, options);
            };
            wrapError(this, options);

            method = this.isNew() ? 'create' : (options.patch ? 'patch' : 'update');
            if (method === 'patch') options.attrs = attrs;
            xhr = this.sync(method, this, options);

            // Restore attributes.
            if (attrs && options.wait) this.attributes = attributes;

            return xhr;
        },

        // Destroy this model on the server if it was already persisted.
        // Optimistically removes the model from its collection, if it has one.
        // If `wait: true` is passed, waits for the server to respond before removal.
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
                if (!model.isNew()) model.trigger('sync', model, resp, options);
            };

            if (this.isNew()) {
                options.success();
                return false;
            }
            wrapError(this, options);

            var xhr = this.sync('delete', this, options);
            if (!options.wait) destroy();
            return xhr;
        },
    })
});
    
//Backbone.Collection 
  
//added in callerName values to manipulate set into sub events systems
var addOptions = { add: true, merge: false, remove: false, callerName:'add' };

// Fetch the default set of models for this collection, resetting the
// collection when they arrive. If `reset: true` is passed, the response
// data will be passed through the `reset` method instead of `set`.
fetch: function(options) {
    options = options ? _.clone(options) : {};
    if (options.parse === void 0) options.parse = true;
    options = _.extend(options,{callerName:'fetch'});
    var success = options.success;
    var collection = this;
    options.success = function(resp) {
        var method = options.reset ? 'reset' : 'set';
        collection[method](resp, options);
        if (success) success(collection, resp, options);
        collection.trigger('sync', collection, resp, options);
    };
    wrapError(this, options);
    return this.sync('read', this, options);
},

// Remove a model, or a list of models from the set.
//added in remove event to be triggerd
        remove: function(models, options) {
            models = _.isArray(models) ? models.slice() : [models];
            options || (options = {});
            var i, l, index, model;
            for (i = 0, l = models.length; i < l; i++) {
                model = this.get(models[i]);
                if (!model) continue;
                delete this._byId[model.id];
                delete this._byId[model.cid];
                index = this.indexOf(model);
                this.models.splice(index, 1);
                this.length--;
                if (!options.silent) {
                    options.index = index;
                    model.trigger('remove', model, this, options);
                    this.trigger('remove'+(options.callerName==undefined ? '' : options.callerName),model,this,options);
                }
                this._removeReference(model);
            }
            return this;
        },

// Update a collection by `set`-ing a new list of models, adding new ones,
// removing models that are no longer present, and merging models that
// already exist in the collection, as necessary. Similar to **Model#set**,
// the core operation for updating the data contained by the collection.
//added code to add the caller name function to any add/remove events in order to allow more focused collection events
set: function(models, options) {
    options = _.defaults(options || {}, setOptions);
    if (options.parse) models = this.parse(models, options);
    if (!_.isArray(models)) models = models ? [models] : [];
    var i, l, model, attrs, existing, sort;
    var at = options.at;
    var sortable = this.comparator && (at == null) && options.sort !== false;
    var sortAttr = _.isString(this.comparator) ? this.comparator : null;
    var toAdd = [], toRemove = [], modelMap = {};
    options.callerName = (options.callerName == undefined ? '' : ':'+options.callerName);

    // Turn bare objects into model references, and prevent invalid models
    // from being added.
    for (i = 0, l = models.length; i < l; i++) {
        if (!(model = this._prepareModel(models[i], options))) continue;

        // If a duplicate is found, prevent it from being added and
        // optionally merge it into the existing model.
        if (existing = this.get(model)) {
            if (options.remove) modelMap[existing.cid] = true;
            if (options.merge) {
                existing.set(model.attributes, options);
                if (sortable && !sort && existing.hasChanged(sortAttr)) sort = true;
            }

            // This is a new model, push it to the `toAdd` list.
        } else if (options.add) {
            toAdd.push(model);

            // Listen to added models' events, and index models for lookup by
            // `id` and by `cid`.
            model.on('all', this._onModelEvent, this);
            this._byId[model.cid] = model;
            if (model.id != null) this._byId[model.id] = model;
        }
    }

    // Remove nonexistent models if appropriate.
    if (options.remove) {
        for (i = 0, l = this.length; i < l; ++i) {
            if (!modelMap[(model = this.models[i]).cid]) toRemove.push(model);
        }
        if (toRemove.length) this.remove(toRemove, options);
    }

    // See if sorting is needed, update `length` and splice in new models.
    if (toAdd.length) {
        if (sortable) sort = true;
        this.length += toAdd.length;
        if (at != null) {
            splice.apply(this.models, [at, 0].concat(toAdd));
        } else {
            push.apply(this.models, toAdd);
        }
    }

    // Silently sort the collection if appropriate.
    if (sort) this.sort({ silent: true });

    if (options.silent) return this;

    // Trigger `add` events.
    for (i = 0, l = toAdd.length; i < l; i++) {
        (model = toAdd[i]).trigger('add'+options.callerName, model, this, options);
    }

    // Trigger `sort` if the collection was sorted.
    if (sort) this.trigger('sort', this, options);
    return this;
},
