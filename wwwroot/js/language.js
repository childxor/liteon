/**
 * ไฟล์ JavaScript สำหรับจัดการภาษาในหน้ฟหกดาเว็บ
 */

// ตัวแปรสำหรับเก็บข้อมูลภาษา
var translations = {};
var currentLanguage = "th";

/**
 * ฟังก์ชันสำหรับดึง moduleId จาก URL
 * @returns {string} - moduleId ในรูปแบบ Base64
 */
window.getModuleIdFromUrl = function () {
  // ดึง URL ปัจจุบัน
  var currentUrl = window.location.pathname;

  // แยกส่วนของ URL ด้วยเครื่องหมาย /
  var segments = currentUrl.split("/").filter(function (segment) {
    return segment.length > 0;
  });

  // ดึงส่วนสุดท้ายของ URL
  var lastSegment =
    segments.length > 0 ? segments[segments.length - 1] : "Index";

  // ถ้าเป็น Index หรือไม่มีส่วนสุดท้าย ให้ใช้ค่า MQ== (Base64 ของ "1")
  if (lastSegment === "Index" || lastSegment === "") {
    return "MQ==";
  }

  // ตรวจสอบว่าเป็นตัวเลขหรือไม่
  if (!isNaN(lastSegment) && lastSegment.indexOf("==") === -1) {
    // แปลงเป็น Base64
    try {
      return btoa(lastSegment);
    } catch (e) {
      console.warn("ไม่สามารถแปลง moduleId เป็น Base64 ได้:", e);
      return "MQ=="; // ใช้ค่าเริ่มต้น
    }
  }

  // ถ้าไม่ใช่ Index และไม่ใช่ตัวเลข ให้ใช้ค่าที่ได้
  return lastSegment;
};

// โหลดข้อมูลภาษาเมื่อโหลดหน้าเว็บ
$(document).ready(function () {
  // loadLanguageData(moduleId);

  // เพิ่ม event listener สำหรับปุ่มเปลี่ยนภาษา
  $(".language-switcher").on("click", function () {
    var language = $(this).data("language");
    changeLanguage(language);
  });

  if ($("#languageTable").length > 0) {
    // initializeLanguageTable();

    // Event handlers
    $("#btnAddKeyword").on("click", showAddLanguageModal);
    $("#btnSaveLanguage").on("click", saveLanguage);
    $("#moduleFilter").on("change", function () {
      $("#languageTable").DataTable().ajax.reload();
    });

    // สร้าง focus trap สำหรับ modal
    setupFocusTrap("#languageManagementModal");
    setupFocusTrap("#editLanguageModal");

    // แก้ไขปัญหา aria-hidden
    $("#languageManagementModal").on("hidden.bs.modal", function () {
      // ย้าย focus ไปที่ปุ่มเปิด modal เมื่อ modal ถูกปิด
      $('[data-bs-target="#languageManagementModal"]').focus();
    });

    $("#editLanguageModal").on("hidden.bs.modal", function () {
      // ย้าย focus ไปที่ modal หลักหรือปุ่มเพิ่มคำแปล
      if ($("#languageManagementModal").hasClass("show")) {
        $("#btnAddKeyword").focus();
      } else {
        $('[data-bs-target="#languageManagementModal"]').focus();
      }
    });

    // ปรับแต่ง modal เมื่อเปิดและปิด
    $("#languageManagementModal").on("shown.bs.modal", function () {
      $(this).find(".modal-content").css("transform", "translateY(0)");
      $(this).find(".modal-content").css("opacity", "1");
      // ตั้งค่า focus ไปที่ปุ่มเพิ่มคำแปล
      $("#btnAddKeyword").focus();
    });

    $("#languageManagementModal").on("hide.bs.modal", function () {
      $(this).find(".modal-content").css("transform", "translateY(50px)");
      $(this).find(".modal-content").css("opacity", "0");
      // ล้าง focus จากทุก element ภายใน modal ก่อนปิด
      $(this).find("button, input, select").blur();
    });

    $("#editLanguageModal").on("shown.bs.modal", function () {
      $(this).find(".modal-content").css("transform", "translateY(0)");
      $(this).find(".modal-content").css("opacity", "1");
      // ตั้งค่า focus ไปที่ input แรก
      $(this).find("input:first").focus();
    });

    $("#editLanguageModal").on("hide.bs.modal", function () {
      $(this).find(".modal-content").css("transform", "translateY(50px)");
      $(this).find(".modal-content").css("opacity", "0");
      // ล้าง focus จากทุก element ภายใน modal ก่อนปิด
      $(this).find("button, input, select").blur();
    });
  }
});

/**
 * สร้าง focus trap สำหรับ modal
 * @param {string} modalSelector - selector ของ modal
 */
function setupFocusTrap(modalSelector) {
  $(modalSelector).on("keydown", function (e) {
    // ถ้ากด Tab
    if (e.key === "Tab") {
      var $modal = $(this);
      var $focusableElements = $modal.find(
        'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
      );
      var $firstFocusableElement = $focusableElements.first();
      var $lastFocusableElement = $focusableElements.last();

      // ถ้ากด Shift + Tab และ focus อยู่ที่ element แรก
      if (e.shiftKey && document.activeElement === $firstFocusableElement[0]) {
        e.preventDefault();
        $lastFocusableElement.focus(); // ย้าย focus ไปที่ element สุดท้าย
      }
      // ถ้ากด Tab และ focus อยู่ที่ element สุดท้าย
      else if (
        !e.shiftKey &&
        document.activeElement === $lastFocusableElement[0]
      ) {
        e.preventDefault();
        $firstFocusableElement.focus(); // ย้าย focus ไปที่ element แรก
      }
    }
    // ถ้ากด Escape
    else if (e.key === "Escape") {
      // ปิด modal
      var modalInstance = bootstrap.Modal.getInstance($(modalSelector)[0]);
      if (modalInstance) {
        modalInstance.hide();
      }
    }
  });
}

/**
 * โหลดข้อมูลภาษาจาก API
 * @param {string} moduleId - รหัสโมดูล
 * @param {function} callback - ฟังก์ชันที่จะเรียกหลังจากโหลดข้อมูลภาษาเสร็จ
 */
window.loadLanguageData = function (moduleId, callback) {
  // ตรวจสอบว่ามี moduleId หรือไม่
  if (!moduleId) {
    // ดึง moduleId จาก URL ก่อน
    moduleId = getModuleIdFromUrl();

    // ถ้าไม่มีค่าจาก URL ให้ลองดึงจาก data attribute
    if (!moduleId || moduleId === "MQ==") {
      moduleId =
        $("body").data("module-id") ||
        $("select#languageSelector").data("module-id");
    }

    // ถ้ายังไม่มี moduleId ให้ใช้ค่าเริ่มต้น
    if (!moduleId) {
      console.warn("ไม่พบ moduleId, ใช้ค่าเริ่มต้น");
      moduleId = "MQ=="; // Base64 ของ "1"
    }
  }

  // ตรวจสอบและแปลง moduleId ให้ถูกต้อง
  if (moduleId) {
    // ถ้าเป็น Index ให้แปลงเป็น Base64 ของ "1"
    if (moduleId === "Index") {
      moduleId = "MQ=="; // Base64 ของ "1"
    }

    // ตรวจสอบว่าเป็นตัวเลขหรือไม่ และไม่ใช่ Base64 อยู่แล้ว
    if (!isNaN(moduleId) && moduleId.indexOf("==") === -1) {
      // แปลงเป็น Base64
      try {
        moduleId = btoa(moduleId);
      } catch (e) {
        console.warn("ไม่สามารถแปลง moduleId เป็น Base64 ได้:", e);
        // ใช้ค่าเริ่มต้น
        moduleId = "MQ=="; // Base64 ของ "1"
      }
    }
  }

  console.log("โหลดข้อมูลภาษาสำหรับ moduleId:", moduleId);

  // แสดง loading indicator
  var showLoader = function () {
    if ($(".page-loader").length) {
      $(".page-loader").fadeIn("fast");
    } else {
      // สร้าง loading indicator ชั่วคราวถ้าไม่มี
      $("body").append(
        '<div class="temp-loader" style="position:fixed;top:0;left:0;width:100%;height:100%;background:rgba(0,0,0,0.3);z-index:9999;display:flex;justify-content:center;align-items:center;"><div style="color:white;background:#333;padding:15px;border-radius:5px;">กำลังโหลดข้อมูลภาษา...</div></div>'
      );
    }
  };

  // ซ่อน loading indicator
  var hideLoader = function () {
    if ($(".page-loader").length) {
      $(".page-loader").fadeOut("fast");
    }
    $(".temp-loader").remove();
  };

  // แสดง loading indicator
  showLoader();

  // ลองโหลดข้อมูลจาก localStorage ก่อน
  var cachedData = null;
  try {
    var storedTranslations = localStorage.getItem("translations_" + moduleId);
    var storedLanguage = localStorage.getItem("currentLanguage");

    if (storedTranslations && storedLanguage) {
      cachedData = {
        data: JSON.parse(storedTranslations),
        language: storedLanguage,
      };

      // ใช้ข้อมูลจาก cache ก่อนเพื่อให้แสดงผลเร็วขึ้น
      window.translations = cachedData.data;
      currentLanguage = cachedData.language;
      translatePage();

      // อัปเดต language selector
      if ($("#languageSelector").length) {
        $("#languageSelector").val(currentLanguage);
      }
    }
  } catch (e) {
    console.warn("ไม่สามารถโหลดข้อมูลภาษาจาก localStorage ได้", e);
  }

  // โหลดข้อมูลจาก API
  $.ajax({
    url: "/Authen/GetLanguageData",
    type: "GET",
    data: { moduleId: moduleId },
    dataType: "json",
    success: function (response) {
      if (response && response.data) {
        // บันทึกข้อมูลภาษาลงในตัวแปร global
        window.translations = response.data;
        currentLanguage = response.language;

        // แปลข้อความในหน้าเว็บทันทีหลังจากโหลดข้อมูลภาษา
        translatePage();

        // อัปเดต language selector
        if ($("#languageSelector").length) {
          $("#languageSelector").val(currentLanguage);
        }

        // เก็บข้อมูลภาษาลงใน localStorage เพื่อใช้งานแบบ offline
        try {
          localStorage.setItem(
            "translations_" + moduleId,
            JSON.stringify(response.data)
          );
          localStorage.setItem("currentLanguage", currentLanguage);
        } catch (e) {
          console.warn("ไม่สามารถบันทึกข้อมูลภาษาลงใน localStorage ได้", e);
        }

        // เรียกใช้ callback ถ้ามี
        if (typeof callback === "function") {
          callback(true, response);
        }
      } else {
        console.error(
          "ไม่สามารถโหลดข้อมูลภาษาได้:",
          response ? response.message : "ไม่มีข้อมูลตอบกลับ"
        );

        // ถ้าไม่สามารถโหลดข้อมูลจาก API ได้ แต่มีข้อมูลใน cache ให้ใช้ข้อมูลจาก cache
        if (cachedData) {
          console.log("ใช้ข้อมูลภาษาจาก localStorage แทน");

          // เรียกใช้ callback ถ้ามี
          if (typeof callback === "function") {
            callback(true, {
              data: cachedData.data,
              language: cachedData.language,
            });
          }
        } else {
          // ถ้าไม่มีข้อมูลใน cache ให้แจ้งเตือน
          if (typeof toastr !== "undefined") {
            toastr.error("ไม่สามารถโหลดข้อมูลภาษาได้");
          }

          // เรียกใช้ callback ถ้ามี
          if (typeof callback === "function") {
            callback(false, response);
          }
        }
      }
    },
    error: function (xhr, status, error) {
      console.error("เกิดข้อผิดพลาดในการโหลดข้อมูลภาษา:", error);

      // ถ้าไม่สามารถโหลดข้อมูลจาก API ได้ แต่มีข้อมูลใน cache ให้ใช้ข้อมูลจาก cache
      if (cachedData) {
        console.log("ใช้ข้อมูลภาษาจาก localStorage แทน");

        // เรียกใช้ callback ถ้ามี
        if (typeof callback === "function") {
          callback(true, {
            data: cachedData.data,
            language: cachedData.language,
          });
        }
      } else {
        // ถ้าไม่มีข้อมูลใน cache ให้แจ้งเตือน
        if (typeof toastr !== "undefined") {
          toastr.error("เกิดข้อผิดพลาดในการโหลดข้อมูลภาษา");
        }

        // เรียกใช้ callback ถ้ามี
        if (typeof callback === "function") {
          callback(false, { error: error });
        }
      }
    },
    complete: function () {
      // ซ่อน loading indicator
      hideLoader();
    },
  });
};

/**
 * เปลี่ยนภาษาของหน้าเว็บ
 * @param {string} language - รหัสภาษา (th, en, zh)
 */
function changeLanguage(language) {
  // ตรวจสอบว่ามีการระบุภาษาหรือไม่
  if (!language) {
    console.error("ไม่ได้ระบุภาษาที่ต้องการเปลี่ยน");
    if (typeof toastr !== "undefined") {
      toastr.error("ไม่ได้ระบุภาษาที่ต้องการเปลี่ยน");
    }
    return;
  }

  // ตรวจสอบว่าภาษาที่เลือกเป็นภาษาปัจจุบันหรือไม่
  if (language === currentLanguage) {
    console.log("ภาษาที่เลือกเป็นภาษาปัจจุบันอยู่แล้ว");
    if (typeof toastr !== "undefined") {
      toastr.info("ภาษาที่เลือกเป็นภาษาปัจจุบันอยู่แล้ว");
    }
    return;
  }

  // ดึง moduleId จาก URL ก่อน
  var moduleId = getModuleIdFromUrl();

  // ถ้าไม่มีค่าจาก URL ให้ลองดึงจาก data attribute
  if (!moduleId || moduleId === "MQ==") {
    moduleId =
      $("body").data("module-id") ||
      $("select#languageSelector").data("module-id");
  }

  // ตรวจสอบและแปลง moduleId ให้ถูกต้อง
  if (moduleId) {
    // ถ้าเป็น Index ให้แปลงเป็น Base64 ของ "1"
    if (moduleId === "Index") {
      moduleId = "MQ=="; // Base64 ของ "1"
    }

    // ตรวจสอบว่าเป็นตัวเลขหรือไม่
    if (!isNaN(moduleId) && moduleId.indexOf("==") === -1) {
      // แปลงเป็น Base64
      try {
        moduleId = btoa(moduleId);
      } catch (e) {
        console.warn("ไม่สามารถแปลง moduleId เป็น Base64 ได้:", e);
        // ใช้ค่าเริ่มต้น
        moduleId = "MQ=="; // Base64 ของ "1"
      }
    }
  } else {
    // ถ้าไม่มี moduleId ให้ใช้ค่าเริ่มต้น
    moduleId = "MQ=="; // Base64 ของ "1"
  }

  // แสดง loading indicator
  if ($(".page-loader").length) {
    $(".page-loader").fadeIn("fast");
  } else {
    // สร้าง loading indicator ชั่วคราวถ้าไม่มี
    $("body").append(
      '<div class="temp-loader" style="position:fixed;top:0;left:0;width:100%;height:100%;background:rgba(0,0,0,0.3);z-index:9999;display:flex;justify-content:center;align-items:center;"><div style="color:white;background:#333;padding:15px;border-radius:5px;">กำลังเปลี่ยนภาษา...</div></div>'
    );
  }

  // บันทึกภาษาที่เลือกใน localStorage ก่อน
  localStorage.setItem("selectedLanguage", language);

  console.log("กำลังเปลี่ยนภาษาเป็น:", language, "moduleId:", moduleId);

  $.ajax({
    url: "/Authen/UpdateLanguage",
    type: "POST",
    headers: {
      "X-Requested-With": "XMLHttpRequest",
    },
    data: {
      language: language,
      moduleId: moduleId,
    },
    dataType: "json",
    success: function (response) {
      console.log("ผลการเปลี่ยนภาษา:", response);

      if (response && response.success) {
        currentLanguage = response.language || language;

        // แทนที่จะ reload ทั้งหน้า ให้โหลดข้อมูลภาษาใหม่และแปลหน้า
        loadLanguageData(moduleId, function () {
          translatePage();

          // อัปเดต language selector
          if ($("#languageSelector").length) {
            $("#languageSelector").val(currentLanguage);
          }

          // แสดงข้อความแจ้งเตือน
          if (typeof toastr !== "undefined") {
            toastr.success(
              "เปลี่ยนภาษาเป็น " +
                getLanguageName(currentLanguage) +
                " เรียบร้อยแล้ว"
            );
          }
        });
      } else {
        // อัปเดตตัวแปร currentLanguage
        // $.ajax({
        //     url: '/Authen/GetLanguageData',
        //     type: 'GET',
        //     data: { moduleId: moduleId },
        //     dataType: 'json',
        //     success: function (langResponse) {
        //         console.log('ผลการโหลดข้อมูลภาษา:', langResponse);
        //         if (langResponse && langResponse.data) {
        //             // บันทึกข้อมูลภาษาลงในตัวแปร global
        //             window.translations = langResponse.data;
        //             // แปลข้อความในหน้าเว็บทันที
        //             translatePage();
        //             // อัปเดต language selector
        //             if ($('#languageSelector').length) {
        //                 $('#languageSelector').val(currentLanguage);
        //             }
        //             // เก็บข้อมูลภาษาลงใน localStorage
        //             try {
        //                 localStorage.setItem('translations_' + moduleId, JSON.stringify(langResponse.data));
        //                 localStorage.setItem('currentLanguage', currentLanguage);
        //             } catch (e) {
        //                 console.warn('ไม่สามารถบันทึกข้อมูลภาษาลงใน localStorage ได้', e);
        //             }
        //             // แสดงข้อความแจ้งเตือน
        //             if (typeof toastr !== 'undefined') {
        //                 toastr.success('เปลี่ยนภาษาเป็น ' + getLanguageName(currentLanguage) + ' เรียบร้อยแล้ว');
        //             }
        //         } else {
        //             console.error('ไม่สามารถโหลดข้อมูลภาษาได้');
        //             // แสดงข้อความแจ้งเตือน
        //             if (typeof toastr !== 'undefined') {
        //                 toastr.error('ไม่สามารถโหลดข้อมูลภาษาได้');
        //             }
        //         }
        //     },
        //     error: function (xhr, status, error) {
        //         console.error('เกิดข้อผิดพลาดในการโหลดข้อมูลภาษา:', error);
        //         console.log('รายละเอียดข้อผิดพลาด:', xhr.responseText);
        //         // แสดงข้อความแจ้งเตือน
        //         if (typeof toastr !== 'undefined') {
        //             toastr.error('เกิดข้อผิดพลาดในการโหลดข้อมูลภาษา');
        //         }
        //     },
        //     complete: function () {
        //         // ซ่อน loading indicator
        //         if ($('.page-loader').length) {
        //             $('.page-loader').fadeOut('fast');
        //         }
        //         $('.temp-loader').remove();
        //     }
        // });
      }
    },
    error: function (xhr, status, error) {
      console.error("เกิดข้อผิดพลาดในการเปลี่ยนภาษา:", error);
      console.log("รายละเอียดข้อผิดพลาด:", xhr.responseText);

      // แสดงข้อความแจ้งเตือน
      if (typeof toastr !== "undefined") {
        toastr.error("เกิดข้อผิดพลาดในการเปลี่ยนภาษา");
      }

      // ซ่อน loading indicator
      if ($(".page-loader").length) {
        $(".page-loader").fadeOut("fast");
      }
      $(".temp-loader").remove();
    },
  });
}

/**
 * ฟังก์ชันสำหรับแปลงรหัสภาษาเป็นชื่อภาษา
 * @param {string} langCode - รหัสภาษา (th, en, zh)
 * @returns {string} ชื่อภาษา
 */
function getLanguageName(langCode) {
  const languageNames = {
    th: "ไทย",
    en: "English",
    zh: "中文",
  };
  return languageNames[langCode] || langCode;
}

/**
 * แปลงรหัสภาษาเป็นชื่อภาษา
 * @param {string} langCode - รหัสภาษา
 * @returns {string} - ชื่อภาษา
 */
function getLanguageName(langCode) {
  switch (langCode) {
    case "th":
      return "ภาษาไทย";
    case "en":
      return "English";
    case "zh":
      return "中文";
    default:
      return langCode;
  }
}

/**
 * แปลข้อความตาม keyword
 * @param {string} keyword - คำที่ต้องการแปล
 * @returns {string} - ข้อความที่แปลแล้ว
 */
function translate(keyword) {
  // ถ้าไม่มี keyword ให้คืนค่าว่าง
  if (!keyword) {
    return "";
  }

  // ตรวจสอบว่ามี translations หรือไม่
  if (!window.translations || Object.keys(window.translations).length === 0) {
    console.warn("ไม่มีข้อมูลภาษาสำหรับแปลหน้าเว็บ");

    // ลองโหลดจาก localStorage
    try {
      var moduleId =
        $("body").data("module-id") ||
        $("select#languageSelector").data("module-id") ||
        "MQ==";
      var storedTranslations = localStorage.getItem("translations_" + moduleId);
      if (storedTranslations) {
        window.translations = JSON.parse(storedTranslations);
      } else {
        // ถ้าไม่มีข้อมูลใน localStorage ให้โหลดใหม่
        loadLanguageData(moduleId, function (success) {
          if (success) {
            translatePage();
          }
        });
        return;
      }
    } catch (e) {
      console.warn("ไม่สามารถโหลดข้อมูลภาษาจาก localStorage ได้", e);
      return;
    }
  }

  // ถ้ายังไม่มี translations ให้คืนค่า keyword เดิม
  if (!window.translations || Object.keys(window.translations).length === 0) {
    return keyword;
  }

  // ถ้ามี keyword ใน translations ให้คืนค่าที่แปลแล้ว
  if (window.translations[keyword]) {
    return window.translations[keyword];
  }

  // ถ้าไม่มี keyword ใน translations ให้คืนค่า keyword เดิม
  return keyword;
}

/**
 * แปลข้อความทั้งหมดในหน้าเว็บ
 * ใช้ data-translate attribute เพื่อระบุ keyword
 * ตัวอย่าง: <span data-translate="welcome">Welcome</span>
 */
window.translatePage = function () {
  // ตรวจสอบว่ามี translations หรือไม่
  if (!window.translations || Object.keys(window.translations).length === 0) {
    console.warn("ไม่มีข้อมูลภาษาสำหรับแปลหน้าเว็บ");

    // ลองโหลดจาก localStorage
    try {
      // ดึง moduleId จาก URL ก่อน
      var moduleId = getModuleIdFromUrl();

      // ถ้าไม่มีค่าจาก URL ให้ลองดึงจาก data attribute
      if (!moduleId || moduleId === "MQ==") {
        moduleId =
          $("body").data("module-id") ||
          $("select#languageSelector").data("module-id") ||
          "MQ==";
      }

      var storedTranslations = localStorage.getItem("translations_" + moduleId);
      if (storedTranslations) {
        window.translations = JSON.parse(storedTranslations);
      } else {
        // ถ้าไม่มีข้อมูลใน localStorage ให้โหลดใหม่
        loadLanguageData(moduleId, function (success) {
          if (success) {
            translatePage();
          }
        });
        return;
      }
    } catch (e) {
      console.warn("ไม่สามารถโหลดข้อมูลภาษาจาก localStorage ได้", e);
      return;
    }
  }

  // แปลข้อความใน element
  $("[data-translate]").each(function () {
    var keyword = $(this).data("translate");
    var translatedText = translate(keyword);
    if (translatedText && translatedText !== keyword) {
      $(this).text(translatedText);
    }
  });

  // แปล placeholder
  $("[data-translate-placeholder]").each(function () {
    var keyword = $(this).data("translate-placeholder");
    var translatedText = translate(keyword);
    if (translatedText && translatedText !== keyword) {
      $(this).attr("placeholder", translatedText);
    }
  });

  // แปล title
  $("[data-translate-title]").each(function () {
    var keyword = $(this).data("translate-title");
    var translatedText = translate(keyword);
    if (translatedText && translatedText !== keyword) {
      $(this).attr("title", translatedText);
    }
  });

  // แปล value ของ button
  $(
    'button[data-translate-value], input[type="button"][data-translate-value], input[type="submit"][data-translate-value]'
  ).each(function () {
    var keyword = $(this).data("translate-value");
    var translatedText = translate(keyword);
    if (translatedText && translatedText !== keyword) {
      $(this).val(translatedText);
    }
  });

  // แปล aria-label
  $("[data-translate-aria-label]").each(function () {
    var keyword = $(this).data("translate-aria-label");
    var translatedText = translate(keyword);
    if (translatedText && translatedText !== keyword) {
      $(this).attr("aria-label", translatedText);
    }
  });

  // แปล alt text ของรูปภาพ
  $("img[data-translate-alt]").each(function () {
    var keyword = $(this).data("translate-alt");
    var translatedText = translate(keyword);
    if (translatedText && translatedText !== keyword) {
      $(this).attr("alt", translatedText);
    }
  });

  // แปลข้อความใน option ของ select
  $("select option[data-translate]").each(function () {
    var keyword = $(this).data("translate");
    var translatedText = translate(keyword);
    if (translatedText && translatedText !== keyword) {
      $(this).text(translatedText);
    }
  });

  // แปลข้อความใน label
  $("label[data-translate]").each(function () {
    var keyword = $(this).data("translate");
    var translatedText = translate(keyword);
    if (translatedText && translatedText !== keyword) {
      $(this).text(translatedText);
    }
  });

  // แปลข้อความใน data-original-title (สำหรับ tooltip)
  $("[data-translate-tooltip]").each(function () {
    var keyword = $(this).data("translate-tooltip");
    var translatedText = translate(keyword);
    if (translatedText && translatedText !== keyword) {
      $(this).attr("data-original-title", translatedText);
      // อัปเดต tooltip ถ้ามี
      if ($.fn.tooltip && $(this).data("bs.tooltip")) {
        $(this).tooltip("dispose").tooltip();
      }
    }
  });

  // แปลข้อความใน data-content (สำหรับ popover)
  $("[data-translate-content]").each(function () {
    var keyword = $(this).data("translate-content");
    var translatedText = translate(keyword);
    if (translatedText && translatedText !== keyword) {
      $(this).attr("data-content", translatedText);
      // อัปเดต popover ถ้ามี
      if ($.fn.popover && $(this).data("bs.popover")) {
        $(this).popover("dispose").popover();
      }
    }
  });

  // แปลข้อความใน HTML ที่มี data-translate-html
  $("[data-translate-html]").each(function () {
    var keyword = $(this).data("translate-html");
    var translatedText = translate(keyword);
    if (translatedText && translatedText !== keyword) {
      $(this).html(translatedText);
    }
  });

  // อัปเดต Chart.js ถ้ามี
  try {
    updateCharts();
  } catch (e) {
    console.warn("ไม่สามารถอัปเดต Chart.js ได้:", e);
  }

  // ทริกเกอร์อีเวนต์ custom เพื่อให้ส่วนอื่นๆ ของแอปพลิเคชันรู้ว่ามีการแปลภาษาแล้ว
  $(document).trigger("translation-updated", [currentLanguage]);
};

/**
 * อัปเดต labels ของ chart
 * @param {Object} chart - Chart.js instance
 */
function updateChartLabels(chart) {
  if (!chart) return;

  try {
    // อัปเดต labels ถ้ามี data-translate
    if (chart.config && chart.config.data && chart.config.data.labels) {
      var needsUpdate = false;

      // อัปเดต dataset labels
      if (chart.config.data.datasets) {
        chart.config.data.datasets.forEach(function (dataset) {
          if (dataset.originalLabel) {
            var translatedText = translate(dataset.originalLabel);
            if (translatedText && translatedText !== dataset.originalLabel) {
              dataset.label = translatedText;
              needsUpdate = true;
            }
          } else if (dataset.label) {
            // เก็บ label ต้นฉบับไว้
            dataset.originalLabel = dataset.label;
            var translatedText = translate(dataset.label);
            if (translatedText && translatedText !== dataset.label) {
              dataset.label = translatedText;
              needsUpdate = true;
            }
          }
        });
      }

      // อัปเดตถ้ามีการเปลี่ยนแปลง
      if (needsUpdate) {
        try {
          chart.update();
        } catch (e) {
          console.warn("ไม่สามารถอัปเดต chart ได้:", e);
        }
      }
    }
  } catch (e) {
    console.error("เกิดข้อผิดพลาดในการอัปเดต Chart labels:", e);
  }
}

/**
 * อัปเดตข้อความใน Chart.js
 */
function updateCharts() {
  // ตรวจสอบว่ามี Chart.js หรือไม่
  if (typeof Chart === "undefined") {
    console.warn("Chart.js ไม่ได้ถูกโหลด");
    return;
  }

  try {
    // อัปเดต chart ที่เก็บไว้ใน window object
    if (window.departmentChart && typeof window.departmentChart === "object") {
      updateChartLabels(window.departmentChart);
    }

    if (window.monthlyChart && typeof window.monthlyChart === "object") {
      updateChartLabels(window.monthlyChart);
    }

    // ใน Chart.js เวอร์ชันใหม่ instances เป็น object ไม่ใช่ array
    if (Chart.instances && typeof Chart.instances === "object") {
      try {
        Object.values(Chart.instances).forEach(function (chart) {
          if (chart && typeof chart === "object") {
            updateChartLabels(chart);
          }
        });
      } catch (e) {
        console.warn("ไม่สามารถอัปเดต Chart.instances ได้:", e);
      }
    } else if (typeof Chart.getChart === "function") {
      // สำหรับ Chart.js v3+
      try {
        var charts = document.querySelectorAll("canvas");
        charts.forEach(function (canvas) {
          try {
            var chart = Chart.getChart(canvas.id || canvas);
            if (chart && typeof chart === "object") {
              updateChartLabels(chart);
            }
          } catch (canvasError) {
            console.warn("ไม่สามารถอัปเดต chart บน canvas ได้:", canvasError);
          }
        });
      } catch (e) {
        console.warn("ไม่สามารถอัปเดต charts ด้วย Chart.getChart ได้:", e);
      }
    } else {
      // ถ้าไม่สามารถเข้าถึง instances ได้ ให้ลองหา canvas ที่มี chart
      try {
        var canvases = document.querySelectorAll("canvas");
        canvases.forEach(function (canvas) {
          if (canvas.chart && typeof canvas.chart === "object") {
            updateChartLabels(canvas.chart);
          }
        });
      } catch (e) {
        console.warn("ไม่สามารถอัปเดต canvas.chart ได้:", e);
      }
    }
  } catch (e) {
    console.error("เกิดข้อผิดพลาดในการอัปเดต Chart:", e);
  }
}

/**
 * ฟังก์ชันสำหรับแปลข้อความแบบไดนามิกโดยใช้ API
 * @param {string} keyword - คีย์เวิร์ดที่ต้องการแปล
 * @param {function} callback - ฟังก์ชันที่จะเรียกหลังจากได้ข้อความแปล
 */
function getTranslation(keyword, callback) {
  $.ajax({
    url: "/Attendance/GetTranslation",
    type: "GET",
    data: { keyword: keyword },
    dataType: "json",
    success: function (response) {
      if (response.success) {
        if (callback && typeof callback === "function") {
          callback(response.text);
        }
      }
    },
    error: function (xhr, status, error) {
      console.error("Error getting translation:", error);
      if (callback && typeof callback === "function") {
        callback(keyword);
      }
    },
  });
}

// ฟังก์ชันเริ่มต้นตาราง
function initializeLanguageTable() {
  $("#languageTable").DataTable({
    processing: true,
    serverSide: true,
    ajax: {
      url: "/Authen/GetLanguageData",
      type: "POST",
      data: function (d) {
        d.moduleId = $("#moduleFilter").val();
      },
    },
    columns: [
      { data: "keyword" },
      { data: "moduleId" },
      { data: "th" },
      { data: "en" },
      { data: "zh" },
      {
        data: "recordStatus",
        render: function (data) {
          return data
            ? '<span class="badge bg-success">ใช้งาน</span>'
            : '<span class="badge bg-danger">ยกเลิก</span>';
        },
      },
      {
        data: "id",
        render: function (data, type, row) {
          return `
                        <div class="btn-group">
                            <button type="button" class="btn btn-sm btn-warning" onclick="editLanguage(${data})">
                                <i class="fas fa-edit"></i>
                            </button>
                            <button type="button" class="btn btn-sm btn-danger" onclick="deleteLanguage(${data})">
                                <i class="fas fa-trash"></i>
                            </button>
                        </div>
                    `;
        },
      },
    ],
    order: [[0, "asc"]],
    language: {
      url: "//cdn.datatables.net/plug-ins/1.13.7/i18n/th.json",
    },
  });
}

// แสดง modal เพิ่มคำแปล
function showAddLanguageModal() {
  $("#languageId").val("");
  $("#languageForm")[0].reset();
  $("#editLanguageModal").modal("show");
  $("#editLanguageModalLabel").text("เพิ่มคำแปล");
}

// แสดง modal แก้ไขคำแปล
function editLanguage(id) {
  $.ajax({
    url: "/Authen/GetLanguageById",
    type: "GET",
    data: { id: id },
    success: function (response) {
      if (response.success) {
        var data = response.data;
        $("#languageId").val(data.id);
        $("#keyword").val(data.keyword);
        $("#moduleId").val(data.moduleId);
        $("#th").val(data.th);
        $("#en").val(data.en);
        $("#cn").val(data.cn);
        $("#editLanguageModal").modal("show");
        $("#editLanguageModalLabel").text("แก้ไขคำแปล");
      }
    },
  });
}

// บันทึกข้อมูลคำแปล
function saveLanguage() {
  var formData = $("#languageForm").serialize();
  $.ajax({
    url: "/Authen/SaveLanguage",
    type: "POST",
    data: formData,
    success: function (response) {
      if (response.success) {
        $("#editLanguageModal").modal("hide");
        $("#languageTable").DataTable().ajax.reload();
        toastr.success("บันทึกข้อมูลสำเร็จ");
      } else {
        toastr.error(response.message || "เกิดข้อผิดพลาด");
      }
    },
    error: function () {
      toastr.error("เกิดข้อผิดพลาดในการบันทึกข้อมูล");
    },
  });
}

// ลบคำแปล
function deleteLanguage(id) {
  Swal.fire({
    title: "ยืนยันการลบ",
    text: "คุณต้องการลบคำแปลนี้ใช่หรือไม่?",
    icon: "warning",
    showCancelButton: true,
    confirmButtonText: "ใช่",
    cancelButtonText: "ไม่",
  }).then((result) => {
    if (result.isConfirmed) {
      $.ajax({
        url: "/Authen/DeleteLanguage",
        type: "POST",
        data: { id: id },
        success: function (response) {
          if (response.success) {
            $("#languageTable").DataTable().ajax.reload();
            toastr.success("ลบข้อมูลสำเร็จ");
          } else {
            toastr.error(response.message || "เกิดข้อผิดพลาด");
          }
        },
        error: function () {
          toastr.error("เกิดข้อผิดพลาดในการลบข้อมูล");
        },
      });
    }
  });
}

// เพิ่มโค้ดเพื่อโหลด module menu ใหม่
function reloadModuleMenu() {
  // ส่ง AJAX request เพื่อโหลด module menu ใหม่
  $.ajax({
    url: "/Home/GetModuleMenu",
    type: "GET",
    success: function (response) {
      if (response && response.success) {
        // อัปเดต module menu ใน topbar
        if ($("#moduleMenu").length) {
          $("#moduleMenu").html(response.html);

          // เพิ่ม event listener สำหรับ dropdown toggle หลังจากโหลด menu ใหม่
          initializeBootstrapDropdowns();

          // แปลข้อความใน module menu
          translateDynamicPage();

          console.log("โหลด module menu ใหม่เรียบร้อยแล้ว");
        }
      }
    },
    error: function (xhr, status, error) {
      console.error("ไม่สามารถโหลด module menu ใหม่ได้:", error);
    },
  });
}

// ฟังก์ชันสำหรับเริ่มต้น Bootstrap dropdowns
function initializeBootstrapDropdowns() {
  // ตรวจสอบว่ามี Bootstrap 5 หรือไม่
  if (typeof bootstrap !== "undefined" && bootstrap.Dropdown) {
    // สำหรับ Bootstrap 5
    var dropdownElementList = [].slice.call(
      document.querySelectorAll(".dropdown-toggle")
    );
    dropdownElementList.map(function (dropdownToggleEl) {
      return new bootstrap.Dropdown(dropdownToggleEl);
    });
  } else {
    // สำหรับ jQuery fallback (ถ้าไม่มี Bootstrap 5)
    $(".dropdown-toggle").on("click", function (e) {
      e.preventDefault();
      $(this).next(".dropdown-menu").toggleClass("show");
    });

    // ปิด dropdown เมื่อคลิกที่อื่น
    $(document).on("click", function (e) {
      if (!$(e.target).closest(".dropdown").length) {
        $(".dropdown-menu").removeClass("show");
      }
    });
  }
}
