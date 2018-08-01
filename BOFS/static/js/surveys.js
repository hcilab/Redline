function question() {
    this.id;
    this.type;
    this.text = [];
    this.labels = [];
    this.likert_num;
}

function survey(filename) {
    this.questions = [];
    this.name = "";
    this.instructions = "";

    //this.form; // json object containing form structure

    // read input file for form generation
    this.read_file = function(filename) {

        var xmlHttpRequest = new XMLHttpRequest();

        xmlHttpRequest.open("get", filename, false);
        xmlHttpRequest.send(null);
        this.form = JSON.parse(xmlHttpRequest.responseText);
    };

    this.insert_questions = function() {
        var n = this.questions.length;
        var title = this.name;
        var instr = this.instructions;

        var q = this.questions;

        var form = this.form;

        var self = this;

        $(document).ready(function() {
            //document.getElementById("title").appendChild(document.createTextNode(form.title));
            //document.getElementById("instructions").appendChild(document.createTextNode(form.instructions));

            if (typeof form.instructions === 'undefined') {
                form.instructions = "";
            }

            document.getElementById("instructions").innerHTML = form.instructions;

            var qs = document.getElementById("q_s");

            for (var i = 0; i < form.questions.length; i++) {
                var q = form.questions[i];
                var datatype = document.createElement("input");
                datatype.type = "hidden";
                datatype.id = q.id + "_datatype";
                datatype.value = q.datatype;
                qs.appendChild(datatype);

                // Stuff which is common between all question types.
                var root = document.createElement("div");
                root.className = "question";

                var padding = document.createElement("div");
                padding.className = "padding";

                root.appendChild(padding);
                qs.appendChild(root);

                if (!(typeof q.instructions === 'undefined')) {
                    var instr = document.createElement("div");
                    instr.style.fontWeight = "bold";
                    var br = document.createElement("br");
                    instr.innerHTML = q.instructions;

                    padding.appendChild(instr);
                    padding.appendChild(br);
                }

                if (q.questiontype == "radiogrid") {

                    var table = document.createElement("table");
                    var head = document.createElement("thead");
                    var body = document.createElement("tbody");
                    table.style.border = "0";
                    table.style.width = "100%";

                    var n_col = q.labels.length;

                    var col = document.createElement("col");
                    col.style.width = "30%";

                    table.appendChild(col);

                    for (var j = 0; j < n_col; j++) {
                        col = document.createElement("col");
                        col.style.width = 70 / n_col + "%";
                        table.appendChild(col);
                    }

                    var th = document.createElement("th");
                    head.appendChild(th);
                    if (q.shuffle == "true") {
                        q.q_text = self.shuffle_array(q.q_text);
                    }
                    for (var j = 0; j < q.labels.length; j++) {
                        th = document.createElement("th");
                        th.appendChild(document.createTextNode(q.labels[j]));

                        head.appendChild(th);
                    }

                    table.appendChild(head);

                    for (var j = 0; j < q.q_text.length; j++) {
                        var tr = document.createElement("tr");
                        if (j % 2 == 0) {
                            tr.className = "evenRow";
                        } else {
                            tr.className = "oddRow";
                        }
                        var text = document.createElement("td");
                        text.innerHTML = q.q_text[j].text;

                        var inv = document.createElement("input");
                        inv.type = "hidden";
                        inv.id = q.q_text[j].id;
                        inv.name = q.q_text[j].id;
                        inv.value = "-1";
                        tr.appendChild(inv);

                        body.appendChild(tr);
                        tr.appendChild(text);
                        for (var k = 0; k < q.labels.length; k++) {
                            var rad = document.createElement("td");
                            var rad_div = document.createElement("div");
                            rad_div.style.width = "50px";
                            rad.appendChild(rad_div);
                            rad_div.id = q.q_text[j].id + "_" + k;
                            rad_div.name = q.q_text[j].id + "_" + k;
                            rad_div.style.marginLeft = "auto";
                            rad_div.style.marginRight = "auto";
                            rad_div.style.paddingLeft = "25px";
                            rad_div.style.paddingTop = "10px";
                            tr.appendChild(rad);
                        }
                    }

                    table.appendChild(body);
                    padding.appendChild(table);

                }
                else if (q.questiontype == "radiolist") {

                    var div = document.createElement("div");
                    var n_row = q.labels.length;

                    for (var j = 0; j < q.labels.length; j++) {
                        var p = document.createElement("div");

                        if (q.horizontal != null && q.horizontal == true) {
                            p.style.float = "left";
                            p.style.paddingRight = "20px";
                        }

                        var inv = document.createElement("input");
                        inv.type = "hidden";
                        inv.id = q.id;
                        inv.name = q.id;
                        inv.value = "-1";

                        var rad_div = document.createElement("div");
                        rad_div.style.width = "25px";
                        rad_div.id = q.id + "_" + j;
                        rad_div.name = q.id + "_" + j;
                        rad_div.style.marginLeft = "auto";
                        rad_div.style.marginRight = "auto";
                        rad_div.style.paddingTop = "2px";
                        rad_div.style.paddingLeft = "10px";
                        rad_div.style.float = "left";

                        var clear = document.createElement("div");
                        clear.style.clear = "both";

                        p.appendChild(inv);
                        p.appendChild(rad_div);
                        p.appendChild(document.createTextNode(q.labels[j]));
                        p.appendChild(clear);


                        div.appendChild(p);
                    }

                    var pClear = document.createElement("div");
                    pClear.style.clear = "both";
                    div.appendChild(pClear);

                    padding.appendChild(div);

                } else if (q.questiontype == "checklist") {

                    var div = document.createElement("div");
                    var n_row = q.questions.length;

                    for (var j = 0; j < q.questions.length; j++) {
                        var p = document.createElement("div");

                        if (q.horizontal != null && q.horizontal == true) {
                            p.style.float = "left";
                            p.style.paddingRight = "20px";
                        }

                        var check_div = document.createElement("div");
                        check_div.style.width = "25px";
                        check_div.id = q.questions[j].id;
                        check_div.name = q.questions[j].id;
                        check_div.style.marginLeft = "auto";
                        check_div.style.marginRight = "auto";
                        check_div.style.paddingTop = "2px";
                        check_div.style.paddingLeft = "10px";
                        check_div.style.float = "left";

                        var clear = document.createElement("div");
                        clear.style.clear = "both";

                        p.appendChild(check_div);
                        p.appendChild(document.createTextNode(q.questions[j].text));
                        p.appendChild(clear);

                        div.appendChild(p);
                    }

                    var pClear = document.createElement("div");
                    pClear.style.clear = "both";
                    div.appendChild(pClear);

                    padding.appendChild(div);

                } else if (q.questiontype == "slider") {
                    var p = document.createElement("p");
                    var label = document.createElement("div");
					
					if (typeof q.width === 'undefined') {
						q.width = 400;
					}
					
                    label.style.width = q.width.toString() + "px";
                    var left = document.createElement("div");
                    var right = document.createElement("div");
                    left.style.float = "left";
                    left.appendChild(document.createTextNode(q.left));
                    right.style.float = "right";
                    right.appendChild(document.createTextNode(q.right));

                    var slider = document.createElement("div");
                    slider.id = q.id;
                    slider.name = q.id;

                    padding.appendChild(p);
                    p.appendChild(label);
                    label.appendChild(left);
                    label.appendChild(right);
                    p.appendChild(slider);

                } else if (q.questiontype == "field") {
                    var field = document.createElement("input");
                    field.type = "text";
                    field.autocomplete = "off";
                    field.id = q.id;
                    field.name = q.id;

                    padding.appendChild(field);
                } else if (q.questiontype == "num_field") {
                    var field = document.createElement("div");
                    field.id = q.id;
                    field.name = q.id;

                    padding.appendChild(field);
                } else if (q.questiontype == "drop_down") {
                    var drop = document.createElement("div");
                    drop.id = q.id;
                    drop.name = q.id;

                    padding.appendChild(drop);
                } else if (q.questiontype == "multi_field") {
                    var field = document.createElement("textarea");
                    field.id = q.id;
                    field.type = "text";
                    field.id = q.id;
                    field.name = q.id;

                    padding.appendChild(field);
                } else if (q.questiontype == "textview") {
                    var text_body = document.createElement("div");
                    text_body.innerHTML = q.text;

                    padding.appendChild(text_body);
                }
            }

            var nav_pad = document.createElement("div");
            nav_pad.className = "navigation padding";
            var floatstyle = document.createElement("div");
            floatstyle.style.float = "right";
            var button = document.createElement("input");
            button.id = "btnNext";
            button.name = "btnNext";
            button.type = "button";
            button.value = "Continue";

            var clear = document.createElement("div");
            clear.style.clear = "both";

            var root = document.getElementById("q_s");
            /*floatstyle.appendChild(button);
			nav_pad.appendChild(floatstyle);
			nav_pad.appendChild(clear);
			root.appendChild(nav_pad);*/


        });
    };



    this.initialize_widgets = function() {
        var form = this.form;

        var code = form.code;

        $(document).ready(function() {
            //$('#form').jqxValidator();
            //var validator = $('#form').jqxValidator({rules: []});

            var rules = [];

            for (var i = 0; i < form.questions.length; i++) {
                var q = form.questions[i];

                if (typeof q.required === 'undefined')
                    q.required = "true";

                switch (q.questiontype) {

                    case "radiogrid":
                        var str = q.code;
                        for (var j = 0; j < q.q_text.length; j++) {
                            //rules[rules.length] = {input: q.q_text[j].id+"_result", message: 'This is required', action: 'select', rule: function(input) {return input.value > 0; }};
                            for (var k = 0; k < q.labels.length; k++) {
                                var id = "#" + q.q_text[j].id + "_" + k;
                                $(id).jqxRadioButton({
                                    width: 50,
                                    height: 30,
                                    enableContainerClick: true,
                                    groupName: q.q_text[j].id
                                });

                                //console.log(q.required);
                                if (q.required == "true") {
                                    //console.log("hehehelfisf");
                                    rules[rules.length] = {
                                        input: '#' + q.q_text[j].id + '_' + (q.labels.length - 1),
                                        message: 'field required',
                                        action: 'select',
                                        rule: function(id) {
                                            //console.log(id);
                                            idParts = id[0].id.split("_");
                                            idParts.pop();
                                            id = idParts.join("_");
                                            aaaa = document.getElementById(id);

                                            return aaaa.value > 0;
                                        }
                                    };
                                }

                                var stuff = q.labels.length - 1;

                                $(id).on('change', function(inner_stuff) {
                                    return function(event) {
                                        var tag = this.id;
                                        var vars = tag.split("_");
                                        var k = vars[vars.length - 1];
                                        vars.pop();

                                        var ID = vars.join("_");

                                        var checked = event.args.checked;
                                        if (checked) {
                                            var field = document.getElementById(ID);
                                            field.value = parseInt(k) + 1;

                                            var logField = document.getElementById("gridItemClicks");
                                            logField.value += "{\"id\": \"" + ID + "\", \"time\": \"" + (Date.now() / 1000.0).toString() + "\", \"value\": \"" + field.value.toString() + "\"};";
                                        }

                                        $('#form').jqxValidator('validateInput', "#" + ID + "_" + inner_stuff);
                                    }
                                }(stuff));
                            }
                        }

                        break;

                    case "radiolist":
                        var str = q.code;
                        for (var j = 0; j < q.labels.length; j++) {

                            var id = "#" + q.id + "_" + j;

                            $(id).jqxRadioButton({
                                width: 30,
                                height: 30,
                                enableContainerClick: true,
                                groupName: q.id
                            });

                            var stuff = q.labels.length - 1;

                            $(id).on('change', function(inner_stuff) {
                                return function(event) {
                                    var tag = this.id;
                                    var vars = tag.split("_");
                                    var k = vars[vars.length - 1];
                                    vars.pop();

                                    var ID = vars.join("_");

                                    var checked = event.args.checked;
                                    if (checked) {
                                        var field = document.getElementById(ID);
                                        field.value = parseInt(k) + 1;
                                    }

                                    $('#form').jqxValidator('validateInput', "#" + ID + "_" + inner_stuff);
                                }
                            }(stuff));

                            //console.log(q.required);
                            if (q.required.toString() == "true") {

                                rules[rules.length] = {
                                    input: '#' + q.id + '_' + (q.labels.length - 1),
                                    message: 'field required',
                                    action: 'select',
                                    rule: function(id) {
                                        //console.log(id);
                                        idParts = id[0].id.split("_");
                                        idParts.pop();
                                        id = idParts.join("_");
                                        aaaa = document.getElementById(id);

                                        return aaaa.value > 0;
                                    }
                                };
                            }
                        }

                        break;

                    case "checklist":
                        var str = q.code;
                        for (var j = 0; j < q.questions.length; j++) {

                            var id = "#" + q.questions[j].id;

                            $(id).jqxCheckBox({
                                width: 30,
                                height: 30,
                                enableContainerClick: false
                            });
                        }

                        break;

                    case "slider":
                        var id = "#" + q.id;
						
						if (typeof q.width === 'undefined') {
                            q.width = 400;
                        }
						
                        $(id).jqxSlider({
                            min: 1,
                            max: q.tick_count,
                            ticksFrequency: 1,
                            mode: 'fixed',
                            width: q.width,
                            value: -1,
                            step: 1
                        });
                        break;

                    case "field":
                        var id = "#" + q.id;

                        if (typeof q.width === 'undefined') {
                            q.width = 200;
                        }

                        if (q.required.toString() == "true")
                            rules[rules.length] = {
                                input: id,
                                message: 'This is required',
                                action: 'keyup, blur',
                                rule: 'required'
                            };
                        $(id).jqxInput({
                            placeHolder: q.placeholder,
                            height: 25,
                            width: q.width,
                            minLength: 1
                        });

                        break;

                    case "num_field":
                        var id = "#" + q.id;
                        //if(q.required == "true")
                        //	rules[rules.length] = {input: id, message: 'This is required', action: 'keyup, blur', rule: 'required'};

						if (typeof q.width === 'undefined') {
                            q.width = 200;
                        }
					
                        $(id).jqxNumberInput({
                            height: 25,
                            inputMode: 'simple',
                            spinButtons: q.spinbutton,
                            min: q.min,
                            max: q.max,
                            decimalDigits: q.decimals,
                            width: q.width
                        });
                        break;

                    case "drop_down":
                        var id = "#" + q.id;
                        var w = q.width;
                        var dw = q.dropwidth;
                        var dh = q.dropheight;

                        if (typeof dw === 'undefined') {
                            if (!(typeof w === 'undefined')) {
                                dw = w;
                            } else {
                                dw = 200;
                            }
                        }

                        if (q.required.toString() == "true")
                            rules[rules.length] = {
                                input: id,
                                message: 'This is required',
                                action: 'select',
                                rule: function(input) {
                                    return input.val().length > 0;
                                }
                            };
                        $(id).jqxDropDownList({
                            source: q.items,
                            width: w,
                            dropDownWidth: dw,
                            dropDownHeight: dh
                        });
                        break;

                    case "multi_field":
                        var id = "#" + q.id;

                        if (q.required.toString() == "true")
                            rules[rules.length] = {
                                input: id,
                                message: 'This is required',
                                action: 'keyup, blur',
                                rule: 'required'
                            };
                        $(id).jqxInput({
                            placeHolder: q.placeholder,
                            height: q.height,
                            width: '100%',
                            minLength: 1
                        });
                        break;
                }
            }

            $('#form').jqxValidator({
                rules: rules
            });

            $("#btnNext").jqxButton({
                width: '100'
            }).bind('click', function() {
                $('#form').jqxValidator('validate');
                //console.log($('#form').jqxValidator.rules);
            });
        });

        eval(code);
    };

    this.shuffle_array = function(a) {
        for (var i = 0; i < a.length; i++) {
            var tmp = a[i];
            var r = Math.random() * (a.length - i) + i;
            r = Math.floor(r);
            a[i] = a[r];
            a[r] = tmp;
        }
        return a;
    };

}