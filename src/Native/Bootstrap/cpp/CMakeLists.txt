project(bootstrappercpp)

add_definitions(-DCPPCODEGEN)

set(SOURCES
    ../main.cpp
    ../common.cpp
)

if(WIN32)
    list(APPEND SOURCES
        ../platform.windows.cpp)
elseif(CLR_CMAKE_PLATFORM_UNIX)
    list(APPEND SOURCES
        ../platform.unix.cpp)
endif()

if(WIN32)
    add_compile_options(/Zc:wchar_t)
    add_definitions(-DUNICODE)
    add_definitions(-D_UNICODE)
endif()

add_library(bootstrappercpp STATIC ${SOURCES})

# Install the static bootstrappercpp library
install (TARGETS bootstrappercpp DESTINATION lib)
