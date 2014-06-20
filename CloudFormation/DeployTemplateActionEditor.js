function InitDeployTemplateActionEditor(o) {
    /// <param name="o" value="{modeSelector:'',s3Selector:'',configSelector:'',directSelector:''}"/>

    $(o.modeSelector).change(function () {
        var value = $(this).val();
        if (value == 's3') {
            $(o.s3Selector).show();
            $(o.configSelector).hide();
            $(o.directSelector).hide();
        } else if (value == 'config') {
            $(o.s3Selector).hide();
            $(o.configSelector).show();
            $(o.directSelector).hide();
        } else if (value == 'direct') {
            $(o.s3Selector).hide();
            $(o.configSelector).hide();
            $(o.directSelector).show();
        }
    });

    $(o.modeSelector).change();
}