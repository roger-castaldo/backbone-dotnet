Backbone = _.extend(Backbone,{
    ErrorMessages : {},
    Language : 'en',
    TranslateValidationError : function(errorMessage) {
        var props = ((Backbone.Language)+'.' + errorMessage).split('.');
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
    ShowModelError : function(model) {
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
    Model:_.extend(Backbone.Model,{
        prototype: _.extend(Backbone.Model.prototype,{
            syncSave:function(attrs, options) {
                if (!options) { options = {}; }
                options = _.extend(options, { async: false });
                return this.save(attrs, options);
            },
            syncDestroy:function(options){
                if (!options) { options = {}; }
                options = _.extend(options, { async: false });
                return this.destroy(options);
            },
            _save: function(key, value, options) {
                var attrs, current;

                // Handle both `("key", value)` and `({key: value})` -style calls.
                if (_.isObject(key) || key == null) {
                    attrs = key;
                    options = value;
                } else {
                    attrs = {};
                    attrs[key] = value;
                }
                options = options ? _.clone(options) : {};

                // If we're "wait"-ing to set changed attributes, validate early.
                if (options.wait) {
                    if (!this._validate(attrs, options)) return false;
                    current = _.clone(this.attributes);
                }

                // Regular saves `set` attributes before persisting to the server.
                var silentOptions = _.extend({}, options, { silent: true });
                if (attrs && !this.set(attrs, options.wait ? silentOptions : options)) {
                    return false;
                }

                // After a successful server-side save, the client is (optionally)
                // updated with the server-side state.
                var model = this;
                var success = options.success;
                options.success = function(resp, status, xhr) {
                    var serverAttrs = model.parse(resp, xhr);
                    if (options.wait) {
                        delete options.wait;
                        serverAttrs = _.extend(attrs || {}, serverAttrs);
                    }
                    if (!model.set(serverAttrs, options)) return false;
                    if (success) {
                        success(model, resp);
                    } else {
                        model.trigger('sync', model, resp, options);
                    }
                };

                // Finish configuring and sending the Ajax request.
                options.error = Backbone.wrapError(options.error, model, options);
                var method = this.isNew() ? 'create' : 'update';
                var xhr = (this.sync || Backbone.sync).call(this, method, this, options);
                if (options.wait) this.set(current, silentOptions);
                return xhr;
            },
            _destroy: function(options) {
                options = options ? _.clone(options) : {};
                var model = this;
                var success = options.success;

                var triggerDestroy = function() {
                    model.trigger('destroy', model, model.collection, options);
                };

                if (this.isNew()) {
                    triggerDestroy();
                    return false;
                }

                options.success = function(resp) {
                    if (options.wait) triggerDestroy();
                    if (success) {
                        success(model, resp);
                    } else {
                        model.trigger('sync', model, resp, options);
                    }
                };

                options.error = Backbone.wrapError(options.error, model, options);
                var xhr = (this.sync || Backbone.sync).call(this, 'delete', this, options);
                if (!options.wait) triggerDestroy();
                return xhr;
            }
        })
    })
});