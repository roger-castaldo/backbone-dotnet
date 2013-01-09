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
            _save: function(key, val, options) {
              var attrs, current, done;

              // Handle both `"key", value` and `{key: value}` -style arguments.
              if (key == null || _.isObject(key)) {
                attrs = key;
                options = val;
              } else if (key != null) {
                (attrs = {})[key] = val;
              }
              options = options ? _.clone(options) : {};

              // If we're "wait"-ing to set changed attributes, validate early.
              if (options.wait) {
                if (attrs && !this._validate(attrs, options)) return false;
                current = _.clone(this.attributes);
              }

              // Regular saves `set` attributes before persisting to the server.
              var silentOptions = _.extend({}, options, {silent: true});
              if (attrs && !this.set(attrs, options.wait ? silentOptions : options)) {
                return false;
              }

              // Do not persist invalid models.
              if (!attrs && !this._validate(null, options)) return false;

              // After a successful server-side save, the client is (optionally)
              // updated with the server-side state.
              var model = this;
              var success = options.success;
              options.success = function(resp, status, xhr) {
                done = true;
                var serverAttrs = model.parse(resp);
                if (options.wait) serverAttrs = _.extend(attrs || {}, serverAttrs);
                if (!model.set(serverAttrs, options)) return false;
                if (success) success(model, resp, options);
              };

              // Finish configuring and sending the Ajax request.
              var method = this.isNew() ? 'create' : (options.patch ? 'patch' : 'update');
              if (method == 'patch') options.attrs = attrs;
              var xhr = this.sync(method, this, options);

              // When using `wait`, reset attributes to original values unless
              // `success` has been called already.
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