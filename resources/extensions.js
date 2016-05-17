(function(){
	_.extractUTCDate = function (date) {
		var ret = date;
		if (!(date instanceof Date)) {
			ret = new Date(date);
		}
		return Date.UTC(ret.getUTCFullYear(), ret.getUTCMonth(), ret.getUTCDate(), ret.getUTCHours(), ret.getUTCMinutes(), ret.getUTCSeconds());
	};
	_.extend = function () {
		var deep = (typeof arguments[0] == "boolean" ? arguments[0] == true : false);
		var sindex = (deep ? 2 : 1);
		var obj = arguments[(deep ? 1 : 0)];
		_.each(Array.prototype.slice.call(arguments, 1), function(source) {
			if (source) {
				for (var prop in source) {
					if (obj[prop] != undefined && deep) {
						if (_.isFunction(obj[prop])){
							obj[prop] = source[prop];
						}
						else if (_.isObject(obj[prop])) {
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
        _sync: Backbone.sync,
        sync: function (method, model, options) {
            if (options == undefined) {
                options = {};
            }
            var _success = options.success;
            options.success = function (response,model,options) {
                if (response.Backbone != undefined) {
                    {
                        _.extend(Backbone, response.Backbone);
                        Backbone.trigger('backbone_extension_occurred');
                    }
                }
                response = response.response;
                if (_success!=undefined){_success(response,model,options);}
            }
			var _error=options.error;
			options.error = function (response, model, options) {
                if (response.Backbone != undefined) {
                    {
                        _.extend(Backbone, response.Backbone);
                        Backbone.trigger('backbone_extension_occurred');
                    }
                }
                response = response.response;
                if (_error != undefined) { _error(response, model, options); }
            }
            return Backbone._sync(method, model, options);
        },
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
        },
        DefineErrorMessage: function (language, path, message) {
            if (language == undefined) { throw 'You must supply a language'; }
            if (path == undefined) { throw 'You must supply an exception path'; }
            if (message == undefined) { throw 'You must supply a message'; }
            var props = path.split('.');
            Backbone.ErrorMessages[language] = Backbone.ErrorMessages[language] || {};
            var obj = Backbone.ErrorMessages[language];
            for (var x = 0; x < props.length-1; x++) {
                obj[props[x]] = obj[props[x]] || {};
                obj = obj[props[x]];
            }
            if (typeof message == 'string' || message instanceof String) {
                obj[props[props.length - 1]] = message;
            } else {
                obj[props[props.length - 1]] = _.extend(true, obj[props[props.length - 1]] || {}, message);
            }
        }
	});
	Backbone.View.prototype.initialize = function (options) {
	    if (this.model != undefined) {
	        if (this.model.on != undefined) {
	            this.model.on('change', this.render, this);
	        }
	    } else if (this.collection != undefined) {
	        if (this.collection.on != undefined) {
	            this.collection.on('reset', this.render, this);
	            this.collection.on('sync', this.render, this);
	            this.collection.on('sort', this.render, this);
	            if (this.AddModel != undefined) {
	                this.collection.on('add',this.AddModel,this);
	            }
	            if (this.RemoveModel != undefined) {
	                this.collection.on('remove', this.RemoveModel, this);
	            }
	        }
	    }
	    _.extend(this, _.omit(options, _.keys(this)));
	};
}).call(this);