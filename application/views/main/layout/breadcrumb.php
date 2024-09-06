<style>
    li.breadcrumb-item {
        text-overflow: ellipsis;
        max-width: 250px;
        height: 21px;
        overflow: hidden;
        white-space: nowrap;
    }
</style>
<div class="page-breadcrumb">
    <div class="row">
        <div class="col-md-12 align-self-center">
            <div class="d-flex align-items-center">
                <nav aria-label="breadcrumb">
                    <ol class="breadcrumb">
                        <?php
                        foreach ($breadcrumb as $key) {
                            foreach ($key as $value) {
                                $title = (empty($value['module']) ? $value['name'] : "<a href='" . $value['module'] . "'>" . $value['name'] . "</a>");
                        ?>
                                <li class="breadcrumb-item <?php echo $value['class']; ?>"><?php echo $title; ?></li>
                        <?php
                            }
                        }
                        ?>
                    </ol>
                </nav>
            </div>
        </div>
    </div>
</div>